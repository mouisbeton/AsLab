using System.IO;
using System.Text.Json;
using System.Windows;
using AsLab.Data;

namespace AsLab.Ui;

/// <summary>Mengganti tema terang/gelap (FR-17) dan menyimpan pilihan pengguna.</summary>
public static class TemaManager
{
    private static string FilePreferensi => Path.Combine(Db.FolderData, "preferensi.json");

    public static bool Gelap { get; private set; }

    public static void Terapkan(bool gelap)
    {
        Gelap = gelap;
        var app = Application.Current;

        // 1. Tema bawaan WPF (tombol, input, tabel).
        app.ThemeMode = gelap ? ThemeMode.Dark : ThemeMode.Light;

        // 2. Warna khusus AsLab: tukar WarnaTerang.xaml dengan WarnaGelap.xaml (atau sebaliknya).
        var daftar = app.Resources.MergedDictionaries;
        var warnaLama = daftar.First(d => d.Source != null && d.Source.OriginalString.Contains("Warna"));
        var warnaBaru = new ResourceDictionary
        {
            Source = new Uri(gelap ? "/ui/Tema/WarnaGelap.xaml" : "/ui/Tema/WarnaTerang.xaml", UriKind.Relative),
        };
        daftar[daftar.IndexOf(warnaLama)] = warnaBaru;

        SimpanPreferensi();
    }

    public static bool MuatPreferensi()
    {
        try
        {
            if (!File.Exists(FilePreferensi)) return false;
            var isi = JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(FilePreferensi));
            return isi != null && isi.GetValueOrDefault("temaGelap");
        }
        catch (Exception)
        {
            return false; // file rusak: pakai tema terang
        }
    }

    private static void SimpanPreferensi()
    {
        try
        {
            Directory.CreateDirectory(Db.FolderData);
            File.WriteAllText(FilePreferensi, JsonSerializer.Serialize(new Dictionary<string, bool> { ["temaGelap"] = Gelap }));
        }
        catch (IOException)
        {
            // Gagal menyimpan preferensi tidak perlu menghentikan aplikasi.
        }
    }
}
