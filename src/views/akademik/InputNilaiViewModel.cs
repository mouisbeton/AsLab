using System.Collections.ObjectModel;
using AsLab.Services;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Views.Akademik;

/// <summary>Satu sel skor (satu komponen) di tabel nilai.</summary>
public partial class SelSkor : ObservableObject
{
    public required InfoKomponen Komponen { get; init; }
    public bool Terkunci { get; init; }

    [ObservableProperty] private string teks = "";

    /// <summary>Pesan error sel ini; null berarti valid. Sel merah bila tidak null.</summary>
    [ObservableProperty] private string? error;
}

/// <summary>
/// Satu baris tabel nilai: satu sel per komponen. Setiap kali teks diketik, baris divalidasi (BR-07)
/// dan nilai modulnya dihitung ulang (BR-01) sehingga langsung terlihat.
/// </summary>
public partial class BarisNilaiVm : ObservableObject
{
    public int PraktikanId { get; }
    public string Nrp { get; }
    public string Nama { get; }
    public bool Terkunci { get; }

    /// <summary>Sel[i] untuk komponen ke-i. View membuat satu kolom per sel.</summary>
    public List<SelSkor> Sel { get; } = [];

    [ObservableProperty] private string nilaiModul = "";

    public BarisNilaiVm(BarisNilai data, List<InfoKomponen> komponen, bool terkunci)
    {
        PraktikanId = data.PraktikanId;
        Nrp = data.Nrp;
        Nama = data.Nama;
        Terkunci = terkunci;
        for (var i = 0; i < komponen.Count; i++)
        {
            var sel = new SelSkor { Komponen = komponen[i], Terkunci = terkunci, Teks = KeTeks(data.Skor[i]) };
            sel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SelSkor.Teks)) Validasi();
            };
            Sel.Add(sel);
        }
        Validasi();
    }

    // 80.00 ditampilkan sebagai "80", 85.50 tetap "85.50".
    private static string KeTeks(decimal? nilai) => nilai.HasValue ? Format.Angka(nilai).Replace(".00", "") : "";

    /// <summary>Baris tanpa isi sama sekali dilewati saat menyimpan.</summary>
    public bool Kosong => Sel.All(s => s.Teks.Trim() == "");

    public bool Valid => Sel.All(s => s.Error == null);

    private void Validasi()
    {
        if (Kosong)
        {
            foreach (var s in Sel) s.Error = null;
            NilaiModul = "";
            return;
        }

        var isi = new List<(decimal Skor, decimal Bobot)>();
        foreach (var s in Sel)
        {
            s.Error = Perhitungan.ValidasiKomponen(s.Teks, out var skor);
            isi.Add((skor, s.Komponen.Bobot));
        }
        NilaiModul = Valid ? Format.Angka(Perhitungan.NilaiModul(isi)) : "–";
    }

    public InputNilai KeInput()
    {
        var skor = new Dictionary<int, decimal>();
        foreach (var s in Sel)
        {
            Format.CobaBacaAngka(s.Teks, out var angka);
            skor[s.Komponen.Id] = angka;
        }
        return new InputNilai(PraktikanId, skor);
    }
}

/// <summary>Input Nilai (FR-07, FR-11): tabel seperti spreadsheet dengan hitung otomatis dan validasi.</summary>
public partial class InputNilaiViewModel : HalamanViewModel
{
    public override string Judul => "Input Nilai";

    public List<OpsiKelas> DaftarKelas { get; } = [];
    public ObservableCollection<OpsiModul> DaftarModul { get; } = [];
    public ObservableCollection<BarisNilaiVm> Baris { get; } = [];

    [ObservableProperty] private OpsiKelas? kelasTerpilih;
    [ObservableProperty] private OpsiModul? modulTerpilih;
    [ObservableProperty] private string infoBobot = "";

    /// <summary>Komponen nilai mata praktikum kelas terpilih. View membuat satu kolom per komponen.</summary>
    [ObservableProperty] private List<InfoKomponen> komponen = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BisaDiubah))]
    private bool terkunci;

    public bool BisaDiubah => !Terkunci;

    public InputNilaiViewModel()
    {
        Coba(() => DaftarKelas.AddRange(KelasService.UntukPengguna()));
        KelasTerpilih = DaftarKelas.FirstOrDefault();
    }

    partial void OnKelasTerpilihChanged(OpsiKelas? value)
    {
        DaftarModul.Clear();
        if (value == null) return;
        Muat(() =>
        {
            foreach (var m in KelasService.ModulKelas(value.Id)) DaftarModul.Add(m);
        });
        var hariIni = DateOnly.FromDateTime(DateTime.Today);
        ModulTerpilih = DaftarModul.LastOrDefault(m => m.Tanggal <= hariIni) ?? DaftarModul.FirstOrDefault();
    }

    partial void OnModulTerpilihChanged(OpsiModul? value) => MuatBaris();

    private void MuatBaris()
    {
        Baris.Clear();
        if (KelasTerpilih == null || ModulTerpilih == null) return;

        Muat(() =>
        {
            var lembar = NilaiService.Ambil(KelasTerpilih.Id, ModulTerpilih.Id);
            Terkunci = lembar.Terkunci;
            InfoBobot = "Bobot: " + string.Join("  ·  ", lembar.Komponen.Select(k => k.Teks));
            Komponen = lembar.Komponen;
            foreach (var data in lembar.Baris)
                Baris.Add(new BarisNilaiVm(data, lembar.Komponen, lembar.Terkunci));
        });
    }

    [RelayCommand]
    private void Simpan()
    {
        if (KelasTerpilih == null || ModulTerpilih == null) return;

        // TC-03: bila ada sel merah, tidak ada yang disimpan.
        var jumlahSalah = Baris.Count(b => !b.Valid);
        if (jumlahSalah > 0)
        {
            PesanSukses = null;
            PesanError = $"{jumlahSalah} baris berisi nilai tidak valid (ditandai merah; arahkan mouse ke sel untuk melihat alasannya). Tidak ada nilai yang disimpan.";
            return;
        }

        var data = Baris.Where(b => !b.Kosong).Select(b => b.KeInput()).ToList();
        var jumlah = 0;
        if (Coba(() => jumlah = NilaiService.Simpan(KelasTerpilih.Id, ModulTerpilih.Id, data)))
        {
            PesanSukses = jumlah == 0 ? "Tidak ada perubahan." : $"{jumlah} nilai disimpan.";
            MuatBaris();
        }
    }
}
