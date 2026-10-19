using System.Globalization;

namespace AsLab.Services.Penilaian;

/// <summary>Satu komponen nilai sebuah mata praktikum, misalnya ("Pretest", 20).</summary>
public record InfoKomponen(int Id, string Nama, decimal Bobot)
{
    public string Teks => string.Create(CultureInfo.InvariantCulture, $"{Nama} {Bobot:0.##}%");
}

/// <summary>
/// Semua rumus penilaian (PRD Bagian 7). Fungsi di sini tidak menyentuh database,
/// sehingga mudah dites (lihat tests/PerhitunganTests.cs).
/// </summary>
public static class Perhitungan
{
    public const decimal BatasKehadiran = 75; // BR-04, BR-05
    public const decimal BatasLulus = 56;     // BR-04, BR-05 (huruf C)

    public const string StatusBerjalan = "Berjalan";
    public const string StatusLulus = "Lulus";
    public const string StatusTidakLulus = "Tidak Lulus";
    public const string StatusTms = "Tidak Memenuhi Syarat";

    /// <summary>BR-12: bulatkan ke 2 desimal (0.005 dibulatkan ke atas).</summary>
    public static decimal Bulatkan(decimal n) => Math.Round(n, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// BR-01: nilai modul = jumlah (skor × bobot) ÷ 100 untuk semua komponen.
    /// Contoh: skor 80/90/70 dengan bobot 20/50/30 → (80×20 + 90×50 + 70×30) ÷ 100 = 82.
    /// </summary>
    public static decimal NilaiModul(IEnumerable<(decimal Skor, decimal Bobot)> isi)
    {
        var total = isi.Sum(x => x.Skor * x.Bobot);
        return Bulatkan(total / 100);
    }

    /// <summary>BR-02: rata-rata dari SELURUH modul; modul tanpa nilai dihitung 0.</summary>
    public static decimal NilaiAkhir(IEnumerable<decimal> nilaiYangAda, int jumlahModul)
    {
        if (jumlahModul == 0) return 0;
        return Bulatkan(nilaiYangAda.Sum() / jumlahModul);
    }

    /// <summary>BR-02: rata-rata dari modul yang sudah dinilai di kelas; null bila belum ada.</summary>
    public static decimal? NilaiSementara(IEnumerable<decimal> nilaiYangAda, int jumlahModulDinilai)
    {
        if (jumlahModulDinilai == 0) return null;
        return Bulatkan(nilaiYangAda.Sum() / jumlahModulDinilai);
    }

    /// <summary>BR-03: Hadir ÷ (Hadir + Alpa). Izin dan Sakit tidak dihitung. Null bila belum ada data.</summary>
    public static decimal? Kehadiran(int hadir, int alpa)
    {
        if (hadir + alpa == 0) return null;
        return Bulatkan(hadir * 100m / (hadir + alpa));
    }

    /// <summary>BR-06: batas bawah termasuk, batas atas tidak termasuk.</summary>
    public static string Huruf(decimal n)
    {
        if (n >= 86) return "A";
        if (n >= 76) return "AB";
        if (n >= 66) return "B";
        if (n >= 61) return "BC";
        if (n >= 56) return "C";
        if (n >= 41) return "D";
        return "E";
    }

    /// <summary>BR-04: status kelulusan.</summary>
    public static string Status(decimal? kehadiran, decimal nilaiAkhir, bool praktikumSelesai)
    {
        if (!praktikumSelesai) return StatusBerjalan;
        if (kehadiran < BatasKehadiran) return StatusTms;
        return nilaiAkhir >= BatasLulus ? StatusLulus : StatusTidakLulus;
    }

    /// <summary>BR-05: berisiko bila kehadiran &lt; 75% atau nilai sementara &lt; 56.</summary>
    public static bool Berisiko(decimal? kehadiran, decimal? nilaiSementara) =>
        kehadiran < BatasKehadiran || nilaiSementara < BatasLulus;

    /// <summary>BR-07: skor komponen 0–100, maksimal 2 desimal. Mengembalikan pesan error atau null bila valid.</summary>
    public static string? ValidasiKomponen(string? teks, out decimal nilai)
    {
        if (string.IsNullOrWhiteSpace(teks))
        {
            nilai = 0;
            return "Lengkapi semua komponen nilai.";
        }
        if (!Format.CobaBacaAngka(teks, out nilai))
            return "Harus berupa angka, contoh 80 atau 85.5.";
        return ValidasiKomponen(nilai);
    }

    public static string? ValidasiKomponen(decimal nilai)
    {
        if (nilai < 0 || nilai > 100) return "Nilai harus di antara 0 dan 100.";
        if (decimal.Round(nilai, 2) != nilai) return "Maksimal 2 angka desimal.";
        return null;
    }

    /// <summary>BR-01: minimal satu komponen, nama terisi dan tidak kembar, bobot di atas 0, total bobot tepat 100.</summary>
    public static string? ValidasiKomponenPraktikum(List<(string Nama, decimal Bobot)> daftar)
    {
        if (daftar.Count == 0)
            return "Minimal harus ada satu komponen nilai.";
        if (daftar.Any(k => k.Nama.Trim() == ""))
            return "Nama komponen tidak boleh kosong.";
        var namaKembar = daftar.GroupBy(k => k.Nama.Trim(), StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (namaKembar != null)
            return $"Nama komponen \"{namaKembar.Key}\" dipakai lebih dari sekali.";
        if (daftar.Any(k => k.Bobot <= 0 || k.Bobot > 100))
            return "Bobot setiap komponen harus lebih dari 0 dan paling besar 100.";
        var total = daftar.Sum(k => k.Bobot);
        if (total != 100)
            return $"Total bobot harus 100 (sekarang {total.ToString("0.##", CultureInfo.InvariantCulture)}).";
        return null;
    }
}
