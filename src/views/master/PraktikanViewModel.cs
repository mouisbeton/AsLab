using System.Collections.ObjectModel;
using AsLab.Services.Master;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Views.Master;

/// <summary>Data Praktikan (FR-03, FR-09, FR-12): tabel, pencarian, filter, paging, form, import CSV.</summary>
public partial class PraktikanViewModel : HalamanViewModel
{
    private const int UkuranHalaman = 20;
    private List<BarisPraktikan> semua = [];
    private List<BarisPraktikan> hasilFilter = [];

    public override string Judul => "Data Praktikan";

    public ObservableCollection<BarisPraktikan> Tabel { get; } = [];
    public List<Opsi> OpsiKelas { get; } = [new(null, "Semua kelas")];
    public ObservableCollection<string> OpsiAngkatan { get; } = ["Semua angkatan"];
    public List<string> OpsiStatus { get; } = ["Semua status", "Berisiko", "Aman"];
    public List<Opsi> OpsiPraktikumImport { get; } = [new(null, "— tanpa kelas —")];

    // Filter (FR-09). Setiap perubahan langsung menerapkan filter.
    [ObservableProperty] private string cari = "";
    [ObservableProperty] private Opsi? filterKelas;
    [ObservableProperty] private string filterAngkatan = "Semua angkatan";
    [ObservableProperty] private string filterStatus = "Semua status";

    // Paging
    [ObservableProperty] private int halamanKe = 1;
    [ObservableProperty] private string infoHalaman = "";

    [ObservableProperty] private BarisPraktikan? terpilih;
    [ObservableProperty] private Opsi? praktikumImport;

    // Form tambah/edit
    [ObservableProperty] private bool formTerbuka;
    [ObservableProperty] private string judulForm = "";
    [ObservableProperty] private string formNrp = "";
    [ObservableProperty] private string formNama = "";
    [ObservableProperty] private string formAngkatan = "";
    [ObservableProperty] private string formEmail = "";
    private int? formId;

    public PraktikanViewModel()
    {
        Coba(() =>
        {
            foreach (var k in KelasService.UntukPengguna()) OpsiKelas.Add(new Opsi(k.Id, k.Label));
            foreach (var p in PraktikumService.Semua()) OpsiPraktikumImport.Add(new Opsi(p.Id, p.Label));
        });
        FilterKelas = OpsiKelas[0];
        PraktikumImport = OpsiPraktikumImport.Count > 1 ? OpsiPraktikumImport[1] : OpsiPraktikumImport[0];
        MuatData();
    }

    private void MuatData()
    {
        Muat(() =>
        {
            semua = PraktikanService.Semua();
            foreach (var angkatan in semua.Select(p => p.Angkatan.ToString()).Distinct().Order())
                if (!OpsiAngkatan.Contains(angkatan)) OpsiAngkatan.Add(angkatan);
        });
        TerapkanFilter();
    }

    partial void OnCariChanged(string value) => TerapkanFilter();
    partial void OnFilterKelasChanged(Opsi? value) => TerapkanFilter();
    partial void OnFilterAngkatanChanged(string value) => TerapkanFilter();
    partial void OnFilterStatusChanged(string value) => TerapkanFilter();

    private void TerapkanFilter()
    {
        IEnumerable<BarisPraktikan> data = semua;

        var kataKunci = Cari.Trim();
        if (kataKunci != "")
            data = data.Where(p => p.Nrp.Contains(kataKunci) || p.Nama.Contains(kataKunci, StringComparison.OrdinalIgnoreCase));
        if (FilterKelas?.Id != null)
            data = data.Where(p => p.KelasIds.Contains(FilterKelas.Id.Value));
        if (FilterAngkatan != "Semua angkatan")
            data = data.Where(p => p.Angkatan.ToString() == FilterAngkatan);
        if (FilterStatus == "Berisiko")
            data = data.Where(p => p.Berisiko);
        else if (FilterStatus == "Aman")
            data = data.Where(p => !p.Berisiko);

        hasilFilter = data.ToList();
        HalamanKe = 1;
        TampilkanHalaman();
    }

    private int JumlahHalaman => Math.Max(1, (int)Math.Ceiling(hasilFilter.Count / (double)UkuranHalaman));

    private void TampilkanHalaman()
    {
        Tabel.Clear();
        foreach (var p in hasilFilter.Skip((HalamanKe - 1) * UkuranHalaman).Take(UkuranHalaman))
            Tabel.Add(p);
        InfoHalaman = $"Halaman {HalamanKe} dari {JumlahHalaman} · {hasilFilter.Count} praktikan";
    }

    [RelayCommand]
    private void Sebelumnya()
    {
        if (HalamanKe > 1) { HalamanKe--; TampilkanHalaman(); }
    }

    [RelayCommand]
    private void Berikutnya()
    {
        if (HalamanKe < JumlahHalaman) { HalamanKe++; TampilkanHalaman(); }
    }

    [RelayCommand]
    private void Tambah()
    {
        formId = null;
        JudulForm = "Tambah Praktikan";
        FormNrp = FormNama = FormEmail = "";
        FormAngkatan = DateTime.Today.Year.ToString();
        FormTerbuka = true;
    }

    [RelayCommand]
    private void Edit()
    {
        if (Terpilih == null) { PesanError = "Pilih praktikan di tabel terlebih dahulu."; return; }
        formId = Terpilih.Id;
        JudulForm = "Edit Praktikan";
        FormNrp = Terpilih.Nrp;
        FormNama = Terpilih.Nama;
        FormAngkatan = Terpilih.Angkatan.ToString();
        FormEmail = Terpilih.Email;
        FormTerbuka = true;
    }

    [RelayCommand]
    private void SimpanForm()
    {
        var input = new InputPraktikan(formId, FormNrp, FormNama, FormAngkatan, FormEmail);
        if (Coba(() => PraktikanService.Simpan(input), "Data praktikan disimpan."))
        {
            FormTerbuka = false;
            MuatData();
        }
    }

    [RelayCommand]
    private void BatalForm() => FormTerbuka = false;

    [RelayCommand]
    private void Hapus()
    {
        if (Terpilih == null) { PesanError = "Pilih praktikan di tabel terlebih dahulu."; return; }
        if (!Dialog.Konfirmasi($"Hapus {Terpilih.Nrp} – {Terpilih.Nama}?\nAbsensi dan nilai praktikan ini juga akan terhapus.")) return;
        if (Coba(() => PraktikanService.Hapus(Terpilih.Id), "Praktikan dihapus."))
            MuatData();
    }

    [RelayCommand]
    private void Detail()
    {
        if (Terpilih != null) Navigasi.BukaDetailPraktikan(Terpilih.Id);
    }

    [RelayCommand]
    private void Import()
    {
        var file = Dialog.PilihFileBuka("File CSV (*.csv)|*.csv");
        if (file == null) return;

        HasilImport? hasil = null;
        if (!Coba(() => hasil = ImportCsvService.Import(file, PraktikumImport?.Id))) return;

        PesanSukses = $"{hasil!.Berhasil} praktikan berhasil diimpor, {hasil.Gagal.Count} baris gagal.";
        if (hasil.Gagal.Count > 0)
            Dialog.Info("Baris yang gagal diimpor:\n\n" + string.Join("\n", hasil.Gagal.Take(30)) +
                        (hasil.Gagal.Count > 30 ? $"\n… dan {hasil.Gagal.Count - 30} baris lainnya" : ""), "Ringkasan Import");
        MuatData();
    }
}
