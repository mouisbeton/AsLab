using System.IO;
using AsLab.Data;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Tests;

/// <summary>
/// Menjaga src/data/schema.sql (skrip SQL skema, PRD 13.7) tetap sama dengan model EF Core.
/// Bila model diubah, jalankan tes dengan variabel ASLAB_TULIS_SKEMA=1 untuk menulis ulang file.
/// </summary>
public class SkemaTests : IDisposable
{
    private readonly DbUji dbUji = new();

    public void Dispose() => dbUji.Dispose();

    [Fact]
    public void SchemaSql_SamaDenganModel()
    {
        using var db = Db.Buka();
        var skrip = db.Database.GenerateCreateScript().ReplaceLineEndings("\n").Trim() + "\n";
        var path = Path.Combine(CariFolderProyek(), "src", "data", "schema.sql");

        if (Environment.GetEnvironmentVariable("ASLAB_TULIS_SKEMA") == "1")
            File.WriteAllText(path, "-- Dibuat otomatis dari AsLabDbContext (lihat tests/AsLab.Tests/SkemaTests.cs).\n" + skrip);

        var isiFile = File.ReadAllText(path).ReplaceLineEndings("\n");
        Assert.EndsWith(skrip, isiFile);
    }

    private static string CariFolderProyek()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder != null && !File.Exists(Path.Combine(folder.FullName, "AsLab.slnx")))
            folder = folder.Parent;
        return folder?.FullName ?? throw new InvalidOperationException("Folder proyek tidak ditemukan.");
    }
}
