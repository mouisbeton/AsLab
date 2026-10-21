using System.IO;
using AsLab.Services.Laporan;
using AsLab.Services.Penilaian;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace AsLab.Tests;

/// <summary>
/// Membuat contoh hasil export untuk dokumentasi (docs/contoh-export) dari data contoh Kelas A.
/// Hanya menulis file bila variabel ASLAB_TULIS_CONTOH=1; tanpa itu tes hanya memastikan export berhasil.
/// </summary>
public class ContohDokumenTests : IDisposable
{
    private readonly DbUji dbUji = new();

    public void Dispose() => dbUji.Dispose();

    [Fact]
    public void ContohExport_Kelas_A()
    {
        DbUji.MasukSebagai("koordinator");
        var rekap = RekapService.Hitung(DbUji.IdKelas("A"));
        var tulis = Environment.GetEnvironmentVariable("ASLAB_TULIS_CONTOH") == "1";
        var folder = tulis
            ? Path.Combine(SkemaTests.CariFolderProyek(), "docs", "contoh-export")
            : Path.Combine(Path.GetTempPath(), $"aslab-contoh-{Guid.NewGuid()}");
        Directory.CreateDirectory(folder);

        try
        {
            ExportService.KeExcel(rekap, Path.Combine(folder, "rekap-PSD-kelas-A.xlsx"));
            ExportService.KePdf(rekap, Path.Combine(folder, "rekap-PSD-kelas-A.pdf"));
            ExportService.BuatDokumenPdf(rekap).GenerateImages(
                i => Path.Combine(folder, $"rekap-PSD-kelas-A-halaman{i + 1}.png"),
                new ImageGenerationSettings { RasterDpi = 110 });

            Assert.True(new FileInfo(Path.Combine(folder, "rekap-PSD-kelas-A.pdf")).Length > 1000);
            Assert.True(File.Exists(Path.Combine(folder, "rekap-PSD-kelas-A-halaman1.png")));
        }
        finally
        {
            if (!tulis) Directory.Delete(folder, recursive: true);
        }
    }
}
