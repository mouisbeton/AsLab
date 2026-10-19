using AsLab.Services.Penilaian;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AsLab.Services.Laporan;

/// <summary>
/// Export rekap nilai per kelas ke Excel dan PDF (FR-13). Isi dan urutan kolom
/// keduanya sama dengan tabel di halaman Rekap (TC-08).
/// </summary>
public static class ExportService
{
    public static List<string> Kolom(RekapKelas rekap)
    {
        var kolom = new List<string> { "No", "NRP", "Nama" };
        kolom.AddRange(rekap.LabelModul);
        kolom.AddRange(["Sementara", "Nilai Akhir", "Huruf", "Kehadiran", "Status"]);
        return kolom;
    }

    public static List<string> Isi(BarisRekap baris, int nomor)
    {
        var isi = new List<string> { nomor.ToString(), baris.Nrp, baris.Nama };
        isi.AddRange(baris.NilaiPerModul.Select(n => n.HasValue ? Format.Angka(n) : ""));
        isi.AddRange([Format.Angka(baris.NilaiSementara), Format.Angka(baris.NilaiAkhir), baris.Huruf, baris.KehadiranTeks, baris.Status]);
        return isi;
    }

    private static string Keterangan(RekapKelas rekap) =>
        $"Kelas {rekap.NamaKelas} · {rekap.Semester} {rekap.TahunAjaran} · Dosen: {rekap.NamaDosen} · " +
        $"Asisten: {(rekap.NamaAsisten.Count == 0 ? "–" : string.Join(", ", rekap.NamaAsisten))}";

    public static void KeExcel(RekapKelas rekap, string pathFile)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Rekap");

        sheet.Cell(1, 1).Value = "Rekap Nilai Praktikum";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;
        sheet.Cell(2, 1).Value = $"{rekap.KodePraktikum} – {rekap.NamaPraktikum}";
        sheet.Cell(3, 1).Value = Keterangan(rekap);
        sheet.Cell(4, 1).Value = "Dicetak: " + Format.Waktu(DateTime.Now) + (rekap.Terkunci ? " · Nilai sudah difinalisasi" : " · Nilai belum difinalisasi");

        var kolom = Kolom(rekap);
        const int barisHeader = 6;
        for (var c = 0; c < kolom.Count; c++)
        {
            var cell = sheet.Cell(barisHeader, c + 1);
            cell.Value = kolom[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#DDE4F5");
        }

        for (var r = 0; r < rekap.Baris.Count; r++)
        {
            var isi = Isi(rekap.Baris[r], r + 1);
            for (var c = 0; c < isi.Count; c++)
            {
                var cell = sheet.Cell(barisHeader + 1 + r, c + 1);
                // Kolom nilai disimpan sebagai angka agar bisa dihitung di Excel; NRP tetap teks.
                var kolomAngka = c >= 3 && c < 3 + rekap.DaftarModul.Count + 2;
                if (kolomAngka && decimal.TryParse(isi[c], System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture, out var angka))
                {
                    cell.Value = angka;
                    cell.Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    cell.Value = isi[c];
                }
            }
        }

        sheet.Range(barisHeader, 1, barisHeader + rekap.Baris.Count, kolom.Count).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        sheet.Range(barisHeader, 1, barisHeader + rekap.Baris.Count, kolom.Count).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        sheet.Columns().AdjustToContents();
        sheet.Column(1).Width = 6;
        workbook.SaveAs(pathFile);
    }

    public static void KePdf(RekapKelas rekap, string pathFile)
    {
        // QuestPDF wajib diberi lisensi sebelum dipakai; Community gratis untuk proyek kuliah.
        QuestPDF.Settings.License = LicenseType.Community;
        var kolom = Kolom(rekap);

        Document.Create(dokumen =>
        {
            dokumen.Page(halaman =>
            {
                halaman.Size(PageSizes.A4.Landscape());
                halaman.Margin(28);
                halaman.DefaultTextStyle(x => x.FontSize(9));

                halaman.Header().PaddingBottom(10).Column(kolomHeader =>
                {
                    kolomHeader.Item().Text("Rekap Nilai Praktikum").FontSize(16).Bold();
                    kolomHeader.Item().Text($"{rekap.KodePraktikum} – {rekap.NamaPraktikum}").FontSize(11);
                    kolomHeader.Item().Text(Keterangan(rekap));
                    kolomHeader.Item().Text("Dicetak: " + Format.Waktu(DateTime.Now) +
                                            (rekap.Terkunci ? " · Nilai sudah difinalisasi" : " · Nilai belum difinalisasi"));
                });

                halaman.Content().Table(tabel =>
                {
                    tabel.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(24);  // No
                        c.ConstantColumn(70);  // NRP
                        c.RelativeColumn(3);   // Nama
                        for (var i = 0; i < kolom.Count - 3; i++) c.RelativeColumn();
                    });

                    tabel.Header(h =>
                    {
                        foreach (var judul in kolom)
                            h.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text(judul).Bold();
                    });

                    for (var r = 0; r < rekap.Baris.Count; r++)
                    {
                        foreach (var teks in Isi(rekap.Baris[r], r + 1))
                            tabel.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).Text(teks);
                    }
                });

                halaman.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Halaman ");
                    t.CurrentPageNumber();
                    t.Span(" dari ");
                    t.TotalPages();
                });
            });
        }).GeneratePdf(pathFile);
    }
}
