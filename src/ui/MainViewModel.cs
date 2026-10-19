using System.Collections.ObjectModel;
using AsLab.Auth;
using AsLab.Data;
using AsLab.Views.Akademik;
using AsLab.Views.Dashboard;
using AsLab.Views.Master;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AsLab.Ui;

/// <summary>Satu item menu di sidebar.</summary>
public partial class ItemMenu : ObservableObject
{
    public required string Judul { get; init; }
    public required string Ikon { get; init; }
    public required string[] Peran { get; init; }
    public required Func<HalamanViewModel> BuatHalaman { get; init; }

    [ObservableProperty]
    private bool aktif;
}

/// <summary>
/// ViewModel jendela utama: menu sidebar sesuai role (FR-02), halaman yang sedang tampil,
/// ganti tema, dan logout.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<ItemMenu> Menu { get; } = [];
    public string NamaPengguna { get; }
    public string PeranPengguna { get; }

    [ObservableProperty]
    private HalamanViewModel? halaman;

    [ObservableProperty]
    private string ikonTema = "";

    /// <summary>Halaman sebelum membuka Detail Praktikan, untuk tombol Kembali.</summary>
    private HalamanViewModel? halamanSebelumnya;

    /// <summary>Dipanggil jendela utama saat pengguna logout.</summary>
    public Action? SaatLogout { get; set; }

    public MainViewModel(string? judulHalamanAwal = null)
    {
        var pengguna = Sesi.Wajib;
        NamaPengguna = pengguna.Nama;
        PeranPengguna = Peran.Label(pengguna.Role);

        const string k = Peran.Koordinator, a = Peran.Asisten, d = Peran.Dosen;
        var semuaMenu = new List<ItemMenu>
        {
            new() { Judul = "Dashboard", Ikon = "", Peran = [k, a, d], BuatHalaman = () => new DashboardViewModel() },
            new() { Judul = "Praktikan", Ikon = "", Peran = [k], BuatHalaman = () => new PraktikanViewModel() },
            new() { Judul = "Kelas & Modul", Ikon = "", Peran = [k], BuatHalaman = () => new KelasModulViewModel() },
            new() { Judul = "Asisten & Akun", Ikon = "", Peran = [k], BuatHalaman = () => new AkunViewModel() },
            new() { Judul = "Absensi", Ikon = "", Peran = [k, a], BuatHalaman = () => new AbsensiViewModel() },
            new() { Judul = "Input Nilai", Ikon = "", Peran = [k, a], BuatHalaman = () => new InputNilaiViewModel() },
            new() { Judul = "Rekap Nilai", Ikon = "", Peran = [k, a, d], BuatHalaman = () => new RekapViewModel() },
            new() { Judul = "Pengaturan", Ikon = "", Peran = [k], BuatHalaman = () => new PengaturanViewModel() },
        };
        foreach (var menu in semuaMenu.Where(m => m.Peran.Contains(pengguna.Role)))
            Menu.Add(menu);

        Navigasi.Utama = this;
        PerbaruiIkonTema();
        var menuAwal = Menu.FirstOrDefault(m => m.Judul.Equals(judulHalamanAwal, StringComparison.OrdinalIgnoreCase)) ?? Menu[0];
        Pilih(menuAwal);
    }

    [RelayCommand]
    private void Pilih(ItemMenu menu)
    {
        foreach (var m in Menu)
            m.Aktif = m == menu;
        halamanSebelumnya = null;
        Halaman = menu.BuatHalaman();
    }

    /// <summary>Membuka halaman di luar menu (Detail Praktikan).</summary>
    public void Buka(HalamanViewModel halamanBaru)
    {
        halamanSebelumnya = Halaman;
        Halaman = halamanBaru;
    }

    public void Kembali()
    {
        if (halamanSebelumnya != null)
        {
            Halaman = halamanSebelumnya;
            halamanSebelumnya = null;
        }
    }

    [RelayCommand]
    private void GantiTema()
    {
        TemaManager.Terapkan(!TemaManager.Gelap);
        PerbaruiIkonTema();
        // Grafik memakai warna teks tema, jadi halaman yang aktif dibuat ulang.
        var menuAktif = Menu.FirstOrDefault(m => m.Aktif);
        if (Halaman is DetailPraktikanViewModel detail)
            Halaman = new DetailPraktikanViewModel(detail.PraktikanId);
        else if (menuAktif != null)
            Halaman = menuAktif.BuatHalaman();
    }

    [RelayCommand]
    private void Logout()
    {
        Sesi.Keluar();
        Navigasi.Utama = null;
        SaatLogout?.Invoke();
    }

    private void PerbaruiIkonTema() => IkonTema = TemaManager.Gelap ? "" : ""; // matahari / bulan
}

/// <summary>Akses ke jendela utama dari halaman lain, misalnya untuk membuka Detail Praktikan.</summary>
public static class Navigasi
{
    public static MainViewModel? Utama { get; set; }

    public static void BukaDetailPraktikan(int praktikanId) => Utama?.Buka(new DetailPraktikanViewModel(praktikanId));

    public static void Kembali() => Utama?.Kembali();
}
