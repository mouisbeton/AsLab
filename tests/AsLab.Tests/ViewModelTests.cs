using AsLab.Services.Penilaian;
using AsLab.Views.Akademik;

namespace AsLab.Tests;

/// <summary>Uji logika baris Input Nilai: validasi per sel (merah) dan nilai modul otomatis.</summary>
public class ViewModelTests
{
    private static readonly List<InfoKomponen> KomponenBawaan =
    [
        new(1, "Pretest", 20),
        new(2, "Praktik", 50),
        new(3, "Laporan", 30),
    ];

    private static BarisNilaiVm BarisKosong(List<InfoKomponen> komponen) =>
        new(new BarisNilai(1, "5025241001", "Ahmad", new decimal?[komponen.Count], null), komponen, terkunci: false);

    [Fact]
    public void NilaiModulLangsungTerhitung()
    {
        var baris = BarisKosong(KomponenBawaan);
        baris.Sel[0].Teks = "80";
        baris.Sel[1].Teks = "90";
        baris.Sel[2].Teks = "70";

        Assert.True(baris.Valid);
        Assert.Equal("82.00", baris.NilaiModul);
        Assert.Equal(new Dictionary<int, decimal> { [1] = 80, [2] = 90, [3] = 70 }, baris.KeInput().Skor);
    }

    [Fact]
    public void TC03_Sel105DitandaiTidakValid()
    {
        var baris = BarisKosong(KomponenBawaan);
        baris.Sel[0].Teks = "105";
        baris.Sel[1].Teks = "90";
        baris.Sel[2].Teks = "70";

        Assert.False(baris.Valid);
        Assert.NotNull(baris.Sel[0].Error);
        Assert.Null(baris.Sel[1].Error);
        Assert.Equal("–", baris.NilaiModul);
    }

    [Fact]
    public void BarisSebagianTerisi_MintaLengkapi_BarisKosongDilewati()
    {
        var baris = BarisKosong(KomponenBawaan);
        Assert.True(baris.Kosong);
        Assert.True(baris.Valid);

        baris.Sel[0].Teks = "80";
        Assert.False(baris.Valid);
        Assert.Equal("Lengkapi semua komponen nilai.", baris.Sel[2].Error);
    }

    [Fact]
    public void JumlahSelMengikutiKomponen()
    {
        var baris = BarisKosong([new(7, "Kuis", 40), new(8, "Proyek", 60)]);
        baris.Sel[0].Teks = "50";
        baris.Sel[1].Teks = "100";

        Assert.Equal(2, baris.Sel.Count);
        Assert.Equal("80.00", baris.NilaiModul);
    }
}
