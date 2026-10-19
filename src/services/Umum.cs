using System.Globalization;

namespace AsLab.Services;

/// <summary>
/// Dilempar saat aturan bisnis dilanggar. Pesannya langsung ditampilkan ke pengguna,
/// jadi tulis pesan yang menjelaskan cara memperbaikinya (NFR-03).
/// </summary>
public class AturanException(string pesan) : Exception(pesan);

/// <summary>Format angka dan tanggal yang dipakai di seluruh aplikasi.</summary>
public static class Format
{
    private static readonly CultureInfo Indonesia = CultureInfo.GetCultureInfo("id-ID");

    // Angka selalu memakai titik desimal, contoh 82.00 (sesuai contoh di PRD).
    public static string Angka(decimal? n) =>
        n.HasValue ? n.Value.ToString("0.00", CultureInfo.InvariantCulture) : "–";

    public static string Persen(decimal? n) => n.HasValue ? Angka(n) + "%" : "–";

    public static string Tanggal(DateOnly t) => t.ToString("dd MMM yyyy", Indonesia);

    public static string Waktu(DateTime t) => t.ToString("dd MMM yyyy HH:mm", Indonesia);

    public static string NamaKelas(string kodePraktikum, string namaKelas) => $"{kodePraktikum} · Kelas {namaKelas}";

    /// <summary>Membaca angka dari teks; menerima "85.5" maupun "85,5".</summary>
    public static bool CobaBacaAngka(string? teks, out decimal hasil)
    {
        hasil = 0;
        if (string.IsNullOrWhiteSpace(teks)) return false;
        return decimal.TryParse(teks.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out hasil);
    }
}
