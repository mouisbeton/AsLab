using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AsLab.Data;

/// <summary>Titik akses database SQL Server. Setiap operasi membuka DbContext baru lewat <see cref="Buka"/>.</summary>
public static class Db
{
    private static DbContextOptions<AsLabDbContext>? _options;

    /// <summary>Folder untuk file milik pengguna seperti preferensi tema.</summary>
    public static string FolderData { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AsLab");

    public static void GunakanSqlServer(string connectionString) => _options = BuatOptions(connectionString);

    public static DbContextOptions<AsLabDbContext> BuatOptions(string connectionString) =>
        new DbContextOptionsBuilder<AsLabDbContext>().UseSqlServer(connectionString).Options;

    public static AsLabDbContext Buka() =>
        new(_options ?? throw new InvalidOperationException("Database belum dikonfigurasi."));

    /// <summary>
    /// Membuat database dan tabel bila belum ada, atau memperbarui strukturnya ke migrasi terbaru
    /// (folder data/Migrasi). Setelah itu mengisi data contoh bila database masih kosong.
    /// </summary>
    public static void Siapkan(bool isiDataContoh = true)
    {
        using var db = Buka();
        db.Database.Migrate();
        if (isiDataContoh && !db.Users.Any())
            Seeder.Isi(db);
    }
}

/// <summary>Dipakai perintah "dotnet ef" (membuat migrasi) untuk membuat DbContext tanpa menjalankan aplikasi.</summary>
public class AsLabDbContextFactory : IDesignTimeDbContextFactory<AsLabDbContext>
{
    public AsLabDbContext CreateDbContext(string[] args) => new(Db.BuatOptions(Konfigurasi.ConnectionString()));
}
