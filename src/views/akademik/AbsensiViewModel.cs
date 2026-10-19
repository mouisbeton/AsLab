using System.Collections.ObjectModel;
using AsLab.Data;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Views.Akademik;

/// <summary>Satu baris absensi; Status berubah saat salah satu tombol H/I/S/A dipilih.</summary>
public partial class BarisAbsensiVm : ObservableObject
{
    public int PraktikanId { get; init; }
    public string Nrp { get; init; } = "";
    public string Nama { get; init; } = "";

    [ObservableProperty] private string? status;
}

/// <summary>Absensi (FR-06): pilih kelas dan modul, isi status, simpan.</summary>
public partial class AbsensiViewModel : HalamanViewModel
{
    public override string Judul => "Absensi";

    public List<OpsiKelas> DaftarKelas { get; } = [];
    public ObservableCollection<OpsiModul> DaftarModul { get; } = [];
    public ObservableCollection<BarisAbsensiVm> Baris { get; } = [];

    [ObservableProperty] private OpsiKelas? kelasTerpilih;
    [ObservableProperty] private OpsiModul? modulTerpilih;
    [ObservableProperty] private string ringkasan = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BisaDiubah))]
    private bool terkunci;

    public bool BisaDiubah => !Terkunci;

    public AbsensiViewModel()
    {
        Coba(() => DaftarKelas.AddRange(KelasService.UntukPengguna()));
        KelasTerpilih = DaftarKelas.FirstOrDefault();
    }

    partial void OnKelasTerpilihChanged(OpsiKelas? value)
    {
        DaftarModul.Clear();
        if (value == null) return;
        Terkunci = value.Terkunci;
        Muat(() =>
        {
            foreach (var m in KelasService.ModulKelas(value.Id)) DaftarModul.Add(m);
        });
        // Pilih modul terakhir yang sudah berlangsung.
        var hariIni = DateOnly.FromDateTime(DateTime.Today);
        ModulTerpilih = DaftarModul.LastOrDefault(m => m.Tanggal <= hariIni) ?? DaftarModul.FirstOrDefault();
    }

    partial void OnModulTerpilihChanged(OpsiModul? value) => MuatBaris();

    private void MuatBaris()
    {
        Baris.Clear();
        if (KelasTerpilih == null || ModulTerpilih == null) { HitungRingkasan(); return; }

        Muat(() =>
        {
            foreach (var data in AbsensiService.Ambil(KelasTerpilih.Id, ModulTerpilih.Id))
            {
                var baris = new BarisAbsensiVm { PraktikanId = data.PraktikanId, Nrp = data.Nrp, Nama = data.Nama, Status = data.Status };
                baris.PropertyChanged += (_, _) => HitungRingkasan();
                Baris.Add(baris);
            }
        });
        HitungRingkasan();
    }

    private void HitungRingkasan()
    {
        var bagian = StatusAbsensi.Semua.Select(s => $"{s} {Baris.Count(b => b.Status == s)}").ToList();
        bagian.Add($"Belum diisi {Baris.Count(b => b.Status == null)}");
        Ringkasan = string.Join("  ·  ", bagian);
    }

    [RelayCommand]
    private void TandaiSemuaHadir()
    {
        foreach (var b in Baris) b.Status = StatusAbsensi.Hadir;
    }

    [RelayCommand]
    private void Simpan()
    {
        if (KelasTerpilih == null || ModulTerpilih == null) return;
        var data = Baris.Where(b => b.Status != null).Select(b => new InputAbsensi(b.PraktikanId, b.Status!)).ToList();
        var jumlah = 0;
        if (Coba(() => jumlah = AbsensiService.Simpan(KelasTerpilih.Id, ModulTerpilih.Id, data)))
        {
            PesanSukses = jumlah == 0 ? "Tidak ada perubahan." : $"{jumlah} status absensi disimpan.";
            MuatBaris();
        }
    }
}
