using System.Collections.ObjectModel;
using AsLab.Data;
using AsLab.Services.Master;
using AsLab.Ui;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Views.Master;

/// <summary>Kotak centang satu kelas pada panel penugasan asisten.</summary>
public partial class PilihanKelas : ObservableObject
{
    public int KelasId { get; init; }
    public string Label { get; init; } = "";

    [ObservableProperty] private bool dipilih;
}

public record OpsiPeran(string Nilai, string Label);

/// <summary>Asisten &amp; Akun (FR-05): kelola akun dan tugaskan asisten ke kelas.</summary>
public partial class AkunViewModel : HalamanViewModel
{
    public override string Judul => "Asisten & Akun";

    public ObservableCollection<BarisAkun> DaftarAkun { get; } = [];
    public ObservableCollection<PilihanKelas> Penugasan { get; } = [];
    public List<OpsiPeran> DaftarPeran { get; } = Peran.Semua.Select(p => new OpsiPeran(p, Peran.Label(p))).ToList();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TampilkanPenugasan))]
    private BarisAkun? akunTerpilih;

    [ObservableProperty] private string judulForm = "Tambah Akun";
    [ObservableProperty] private string formUsername = "";
    [ObservableProperty] private string formNama = "";
    [ObservableProperty] private string formPeran = Peran.Asisten;

    /// <summary>Diisi oleh code-behind karena PasswordBox tidak mendukung binding.</summary>
    public string FormPassword { get; set; } = "";

    /// <summary>Dipanggil untuk mengosongkan PasswordBox di View.</summary>
    public Action? KosongkanPassword { get; set; }

    public bool TampilkanPenugasan => AkunTerpilih?.Role == Peran.Asisten;

    public AkunViewModel()
    {
        MuatAkun(null);
    }

    private void MuatAkun(int? pilihId)
    {
        Muat(() =>
        {
            DaftarAkun.Clear();
            foreach (var a in AkunService.Semua()) DaftarAkun.Add(a);
        });
        AkunTerpilih = DaftarAkun.FirstOrDefault(a => a.Id == pilihId);
        if (AkunTerpilih == null) Baru();
    }

    partial void OnAkunTerpilihChanged(BarisAkun? value)
    {
        Penugasan.Clear();
        if (value == null) return;

        JudulForm = "Edit Akun";
        FormUsername = value.Username;
        FormNama = value.Nama;
        FormPeran = value.Role;
        FormPassword = "";
        KosongkanPassword?.Invoke();

        if (value.Role == Peran.Asisten)
        {
            var kelasAsisten = AkunService.KelasAsisten(value.Id);
            foreach (var k in KelasService.UntukPengguna())
                Penugasan.Add(new PilihanKelas { KelasId = k.Id, Label = k.Label, Dipilih = kelasAsisten.Contains(k.Id) });
        }
    }

    [RelayCommand]
    private void Baru()
    {
        AkunTerpilih = null;
        JudulForm = "Tambah Akun";
        FormUsername = FormNama = "";
        FormPeran = Peran.Asisten;
        FormPassword = "";
        KosongkanPassword?.Invoke();
    }

    [RelayCommand]
    private void Simpan()
    {
        var id = 0;
        var input = new InputAkun(AkunTerpilih?.Id, FormUsername, FormNama, FormPeran, FormPassword);
        if (Coba(() => id = AkunService.Simpan(input), "Akun disimpan."))
            MuatAkun(id);
    }

    [RelayCommand]
    private void Hapus()
    {
        if (AkunTerpilih == null) { PesanError = "Pilih akun di tabel terlebih dahulu."; return; }
        if (!Dialog.Konfirmasi($"Hapus akun {AkunTerpilih.Username}?")) return;
        if (Coba(() => AkunService.Hapus(AkunTerpilih.Id), "Akun dihapus."))
            MuatAkun(null);
    }

    [RelayCommand]
    private void SimpanPenugasan()
    {
        if (AkunTerpilih == null) return;
        var kelasIds = Penugasan.Where(p => p.Dipilih).Select(p => p.KelasId).ToList();
        if (Coba(() => AkunService.SimpanPenugasan(AkunTerpilih.Id, kelasIds), "Penugasan asisten disimpan."))
            MuatAkun(AkunTerpilih.Id);
    }
}
