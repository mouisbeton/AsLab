using System.IO;
using System.Text.Json;

namespace AsLab.Data;

/// <summary>
/// Membaca connection string SQL Server dari appsettings.json di folder program.
/// Variabel lingkungan ASLAB_KONEKSI (bila diisi) menggantikan isi file, misalnya untuk database demo.
/// </summary>
public static class Konfigurasi
{
    public const string KoneksiBawaan =
        @"Server=localhost\SQLEXPRESS;Database=AsLab;Trusted_Connection=True;TrustServerCertificate=True;";

    public static string ConnectionString()
    {
        var dariLingkungan = Environment.GetEnvironmentVariable("ASLAB_KONEKSI");
        if (!string.IsNullOrWhiteSpace(dariLingkungan))
            return dariLingkungan;

        var file = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(file))
            return KoneksiBawaan;

        using var json = JsonDocument.Parse(File.ReadAllText(file));
        return json.RootElement.GetProperty("ConnectionStrings").GetProperty("AsLab").GetString() ?? KoneksiBawaan;
    }
}
