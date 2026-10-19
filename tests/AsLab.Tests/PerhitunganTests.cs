using AsLab.Services.Penilaian;

namespace AsLab.Tests;

/// <summary>Uji rumus penilaian (PRD Bagian 7 dan 14) tanpa database.</summary>
public class PerhitunganTests
{
    [Fact]
    public void TC04_NilaiModul_KomponenBawaan()
    {
        // Pretest 80 (20%), Praktik 90 (50%), Laporan 70 (30%)
        Assert.Equal(82.00m, Perhitungan.NilaiModul([(80, 20), (90, 50), (70, 30)]));
    }

    [Fact]
    public void NilaiModul_JumlahKomponenBebas()
    {
        Assert.Equal(80.00m, Perhitungan.NilaiModul([(100, 10), (80, 20), (60, 30), (90, 40)]));
        Assert.Equal(75.00m, Perhitungan.NilaiModul([(75, 100)]));
    }

    [Fact]
    public void TC06_Alpa3Dari8_TidakMemenuhiSyarat()
    {
        var kehadiran = Perhitungan.Kehadiran(hadir: 5, alpa: 3);

        Assert.Equal(62.50m, kehadiran);
        Assert.Equal(Perhitungan.StatusTms, Perhitungan.Status(kehadiran, nilaiAkhir: 90, praktikumSelesai: true));
        Assert.True(Perhitungan.Berisiko(kehadiran, nilaiSementara: 90));
    }

    [Fact]
    public void TC09_IzinTidakMenurunkanKehadiran()
    {
        // 6 Hadir, 1 Izin, 1 Alpa: Izin tidak dihitung → 6 ÷ 7
        var kehadiran = Perhitungan.Kehadiran(hadir: 6, alpa: 1);

        Assert.Equal(85.71m, kehadiran);
        Assert.False(Perhitungan.Berisiko(kehadiran, nilaiSementara: 70));
    }

    [Theory]
    [InlineData(100, "A")]
    [InlineData(86.00, "A")]
    [InlineData(85.99, "AB")] // TC-10
    [InlineData(76, "AB")]
    [InlineData(75.99, "B")]
    [InlineData(66, "B")]
    [InlineData(65.99, "BC")]
    [InlineData(61, "BC")]
    [InlineData(60.99, "C")]
    [InlineData(56, "C")]
    [InlineData(55.99, "D")]
    [InlineData(41, "D")]
    [InlineData(40.99, "E")]
    [InlineData(0, "E")]
    public void TC10_KonversiHuruf_TanpaCelah(double nilai, string huruf)
    {
        Assert.Equal(huruf, Perhitungan.Huruf((decimal)nilai));
    }

    [Fact]
    public void TC11_ModulTanpaNilaiDihitungNol()
    {
        var akhir = Perhitungan.NilaiAkhir([80, 80, 80, 80], jumlahModul: 5);

        Assert.Equal(64.00m, akhir);
        Assert.Equal("BC", Perhitungan.Huruf(akhir));
    }

    [Fact]
    public void TC12_BobotBaru()
    {
        Assert.Equal(81.00m, Perhitungan.NilaiModul([(80, 30), (90, 40), (70, 30)]));
    }

    [Theory]
    [InlineData("105")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("80.555")]
    [InlineData("")]
    public void TC03_KomponenTidakValid(string teks)
    {
        Assert.NotNull(Perhitungan.ValidasiKomponen(teks, out _));
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("100", 100)]
    [InlineData("85.5", 85.5)]
    [InlineData("85,5", 85.5)]
    public void KomponenValid_TerimaTitikDanKoma(string teks, double harapan)
    {
        Assert.Null(Perhitungan.ValidasiKomponen(teks, out var nilai));
        Assert.Equal((decimal)harapan, nilai);
    }

    [Fact]
    public void Pembulatan_HalfUp()
    {
        Assert.Equal(85.01m, Perhitungan.Bulatkan(85.005m));
        Assert.Equal(85.00m, Perhitungan.Bulatkan(85.004m));
    }

    [Fact]
    public void Kehadiran_TanpaData_TidakBerisiko()
    {
        Assert.Null(Perhitungan.Kehadiran(0, 0));
        Assert.False(Perhitungan.Berisiko(null, null));
    }

    [Fact]
    public void Status_BerjalanSebelumSelesai_LulusTidakLulusSesudahnya()
    {
        Assert.Equal(Perhitungan.StatusBerjalan, Perhitungan.Status(100, 90, praktikumSelesai: false));
        Assert.Equal(Perhitungan.StatusLulus, Perhitungan.Status(80, 56, praktikumSelesai: true));
        Assert.Equal(Perhitungan.StatusTidakLulus, Perhitungan.Status(80, 55.99m, praktikumSelesai: true));
    }

    [Fact]
    public void ValidasiKomponenPraktikum()
    {
        Assert.Null(Perhitungan.ValidasiKomponenPraktikum([("Pretest", 20), ("Praktik", 50), ("Laporan", 30)]));
        Assert.Null(Perhitungan.ValidasiKomponenPraktikum([("Proyek", 100)]));
        Assert.NotNull(Perhitungan.ValidasiKomponenPraktikum([("Pretest", 20), ("Praktik", 50), ("Laporan", 25)])); // total 95
        Assert.NotNull(Perhitungan.ValidasiKomponenPraktikum([("Pretest", 0), ("Praktik", 100)]));                // bobot 0
        Assert.NotNull(Perhitungan.ValidasiKomponenPraktikum([("Kuis", 50), ("KUIS", 50)]));                      // nama kembar
        Assert.NotNull(Perhitungan.ValidasiKomponenPraktikum([(" ", 100)]));                                     // nama kosong
        Assert.NotNull(Perhitungan.ValidasiKomponenPraktikum([]));
    }
}
