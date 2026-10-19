using System.Collections.ObjectModel;
using AsLab.Services.Master;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Views.Master;

/// <summary>Satu baris jadwal yang tanggalnya bisa diubah lewat DatePicker.</summary>
public partial class BarisJadwalVm : ObservableObject
{
    public int ModulId { get; init; }
    public int Nomor { get; init; }
    public string Judul { get; init; } = "";
    public string Label => $"M{Nomor} · {Judul}";

    [ObservableProperty] private DateTime? tanggal;
}

/// <summary>
/// Kelas &amp; Modul (FR-04): kelola mata praktikum, lalu kelas dan modulnya.
/// Memilih kelas menampilkan peserta, asisten, dan jadwal modul kelas tersebut.
/// </summary>
public partial class KelasModulViewModel : HalamanViewModel
{
    public override string Judul => "Kelas & Modul";

    public string[] DaftarSemester => PraktikumService.DaftarSemester;
    public string[] DaftarHari => KelasService.DaftarHari;
    public List<Opsi> DaftarDosen { get; } = [new(null, "— belum ada —")];

    // ===== Mata praktikum =====
    public ObservableCollection<BarisMataPraktikum> DaftarPraktikum { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdaPraktikum))]
    private BarisMataPraktikum? praktikumTerpilih;

    [ObservableProperty] private string mpKode = "";
    [ObservableProperty] private string mpNama = "";
    [ObservableProperty] private string mpSemester = "Ganjil";
    [ObservableProperty] private string mpTahun = "";
    [ObservableProperty] private Opsi? mpDosen;

    public bool AdaPraktikum => PraktikumTerpilih != null;

    // ===== Kelas =====
    public ObservableCollection<BarisKelas> DaftarKelas { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AdaKelas))]
    private BarisKelas? kelasTerpilih;

    [ObservableProperty] private string klsNama = "";
    [ObservableProperty] private string klsHari = "Senin";
    [ObservableProperty] private string klsJam = "";
    [ObservableProperty] private string klsRuang = "";

    public bool AdaKelas => KelasTerpilih != null;

    public ObservableCollection<BarisPeserta> Peserta { get; } = [];
    public ObservableCollection<BarisPeserta> Kandidat { get; } = [];
    public ObservableCollection<BarisJadwalVm> Jadwal { get; } = [];
    [ObservableProperty] private BarisPeserta? pesertaTerpilih;
    [ObservableProperty] private BarisPeserta? kandidatTerpilih;

    // ===== Modul =====
    public ObservableCollection<BarisModul> DaftarModul { get; } = [];
    [ObservableProperty] private BarisModul? modulTerpilih;
    [ObservableProperty] private string mdlNomor = "";
    [ObservableProperty] private string mdlJudul = "";

    public KelasModulViewModel()
    {
        Coba(() =>
        {
            foreach (var d in AkunService.DaftarDosen()) DaftarDosen.Add(new Opsi(d.Id, d.Nama));
        });
        MuatPraktikum(null);
    }

    // ---------- Mata praktikum ----------

    private void MuatPraktikum(int? pilihId)
    {
        DaftarPraktikum.Clear();
        foreach (var p in PraktikumService.Semua()) DaftarPraktikum.Add(p);
        PraktikumTerpilih = DaftarPraktikum.FirstOrDefault(p => p.Id == pilihId) ?? DaftarPraktikum.FirstOrDefault();
        if (PraktikumTerpilih == null) BaruPraktikum();
    }

    partial void OnPraktikumTerpilihChanged(BarisMataPraktikum? value)
    {
        if (value != null)
        {
            MpKode = value.Kode;
            MpNama = value.Nama;
            MpSemester = value.Semester;
            MpTahun = value.TahunAjaran;
            MpDosen = DaftarDosen.FirstOrDefault(d => d.Id == value.DosenId) ?? DaftarDosen[0];
        }
        MuatKelas(null);
        MuatModul(null);
    }

    [RelayCommand]
    private void BaruPraktikum()
    {
        PraktikumTerpilih = null;
        MpKode = MpNama = "";
        MpSemester = "Ganjil";
        MpTahun = $"{DateTime.Today.Year}/{DateTime.Today.Year + 1}";
        MpDosen = DaftarDosen[0];
    }

    [RelayCommand]
    private void SimpanPraktikum()
    {
        var id = 0;
        var input = new InputMataPraktikum(PraktikumTerpilih?.Id, MpKode, MpNama, MpSemester, MpTahun, MpDosen?.Id);
        if (Coba(() => id = PraktikumService.Simpan(input), "Mata praktikum disimpan."))
            MuatPraktikum(id);
    }

    [RelayCommand]
    private void HapusPraktikum()
    {
        if (PraktikumTerpilih == null) return;
        if (!Dialog.Konfirmasi($"Hapus mata praktikum {PraktikumTerpilih.Label}?")) return;
        if (Coba(() => PraktikumService.Hapus(PraktikumTerpilih.Id), "Mata praktikum dihapus."))
            MuatPraktikum(null);
    }

    // ---------- Kelas ----------

    private void MuatKelas(int? pilihId)
    {
        DaftarKelas.Clear();
        if (PraktikumTerpilih != null)
            foreach (var k in KelasService.DaftarKelas(PraktikumTerpilih.Id)) DaftarKelas.Add(k);
        KelasTerpilih = DaftarKelas.FirstOrDefault(k => k.Id == pilihId) ?? DaftarKelas.FirstOrDefault();
        if (KelasTerpilih == null) BaruKelas();
    }

    partial void OnKelasTerpilihChanged(BarisKelas? value)
    {
        if (value != null)
        {
            KlsNama = value.NamaKelas;
            KlsHari = value.Hari;
            KlsJam = value.Jam;
            KlsRuang = value.Ruang;
        }
        MuatDetailKelas();
    }

    private void MuatDetailKelas()
    {
        Peserta.Clear();
        Kandidat.Clear();
        Jadwal.Clear();
        if (KelasTerpilih == null) return;

        foreach (var p in KelasService.Peserta(KelasTerpilih.Id)) Peserta.Add(p);
        foreach (var p in KelasService.KandidatPeserta(KelasTerpilih.Id)) Kandidat.Add(p);
        foreach (var j in KelasService.Jadwal(KelasTerpilih.Id))
        {
            Jadwal.Add(new BarisJadwalVm
            {
                ModulId = j.ModulId, Nomor = j.Nomor, Judul = j.Judul,
                Tanggal = j.Tanggal?.ToDateTime(TimeOnly.MinValue),
            });
        }
    }

    [RelayCommand]
    private void BaruKelas()
    {
        KelasTerpilih = null;
        KlsNama = KlsJam = KlsRuang = "";
        KlsHari = "Senin";
    }

    [RelayCommand]
    private void SimpanKelas()
    {
        if (PraktikumTerpilih == null) { PesanError = "Simpan mata praktikum terlebih dahulu."; return; }
        var id = 0;
        var input = new InputKelas(KelasTerpilih?.Id, PraktikumTerpilih.Id, KlsNama, KlsHari, KlsJam, KlsRuang);
        if (Coba(() => id = KelasService.Simpan(input), "Kelas disimpan."))
            MuatKelas(id);
    }

    [RelayCommand]
    private void HapusKelas()
    {
        if (KelasTerpilih == null) return;
        if (!Dialog.Konfirmasi($"Hapus Kelas {KelasTerpilih.NamaKelas}? Daftar peserta dan jadwalnya ikut terhapus.")) return;
        if (Coba(() => KelasService.Hapus(KelasTerpilih.Id), "Kelas dihapus."))
            MuatKelas(null);
    }

    [RelayCommand]
    private void TambahPeserta()
    {
        if (KelasTerpilih == null || KandidatTerpilih == null) { PesanError = "Pilih praktikan yang akan ditambahkan."; return; }
        if (Coba(() => KelasService.TambahPeserta(KelasTerpilih.Id, KandidatTerpilih.PraktikanId), "Peserta ditambahkan."))
            MuatKelas(KelasTerpilih.Id);
    }

    [RelayCommand]
    private void KeluarkanPeserta()
    {
        if (KelasTerpilih == null || PesertaTerpilih == null) { PesanError = "Pilih peserta di tabel terlebih dahulu."; return; }
        if (!Dialog.Konfirmasi($"Keluarkan {PesertaTerpilih.Label} dari Kelas {KelasTerpilih.NamaKelas}?")) return;
        if (Coba(() => KelasService.KeluarkanPeserta(KelasTerpilih.Id, PesertaTerpilih.PraktikanId), "Peserta dikeluarkan."))
            MuatKelas(KelasTerpilih.Id);
    }

    [RelayCommand]
    private void SimpanJadwal()
    {
        if (KelasTerpilih == null) return;
        var jadwal = Jadwal
            .Select(j => new BarisJadwal(j.ModulId, j.Nomor, j.Judul, j.Tanggal.HasValue ? DateOnly.FromDateTime(j.Tanggal.Value) : null))
            .ToList();
        Coba(() => KelasService.SimpanJadwal(KelasTerpilih.Id, jadwal), "Jadwal modul disimpan.");
    }

    // ---------- Modul ----------

    private void MuatModul(int? pilihId)
    {
        DaftarModul.Clear();
        if (PraktikumTerpilih != null)
            foreach (var m in PraktikumService.DaftarModul(PraktikumTerpilih.Id)) DaftarModul.Add(m);
        ModulTerpilih = DaftarModul.FirstOrDefault(m => m.Id == pilihId) ?? DaftarModul.FirstOrDefault();
        if (ModulTerpilih == null) BaruModul();
    }

    partial void OnModulTerpilihChanged(BarisModul? value)
    {
        if (value == null) return;
        MdlNomor = value.Nomor.ToString();
        MdlJudul = value.Judul;
    }

    [RelayCommand]
    private void BaruModul()
    {
        ModulTerpilih = null;
        MdlNomor = (DaftarModul.Count == 0 ? 1 : DaftarModul.Max(m => m.Nomor) + 1).ToString();
        MdlJudul = "";
    }

    [RelayCommand]
    private void SimpanModul()
    {
        if (PraktikumTerpilih == null) { PesanError = "Simpan mata praktikum terlebih dahulu."; return; }
        var id = 0;
        var input = new InputModul(ModulTerpilih?.Id, PraktikumTerpilih.Id, MdlNomor, MdlJudul);
        if (Coba(() => id = PraktikumService.SimpanModul(input), "Modul disimpan."))
        {
            MuatModul(id);
            MuatDetailKelas(); // jadwal kelas ikut menampilkan modul baru
        }
    }

    [RelayCommand]
    private void HapusModul()
    {
        if (ModulTerpilih == null) return;
        if (!Dialog.Konfirmasi($"Hapus modul {ModulTerpilih.Label}?")) return;
        if (Coba(() => PraktikumService.HapusModul(ModulTerpilih.Id), "Modul dihapus."))
        {
            MuatModul(null);
            MuatDetailKelas();
        }
    }
}
