using System.IO;
using AsLab.Auth;
using AsLab.Data;
using AsLab.Services;
using AsLab.Services.Dashboard;
using AsLab.Services.Laporan;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;
using AsLab.Services.Pengaturan;
using ClosedXML.Excel;

namespace AsLab.Tests;

/// <summary>Uji skenario PRD Bagian 14 melalui service, memakai database contoh di memori.</summary>
public class LayananTests : IDisposable
{
    private readonly DbUji dbUji = new();

    public void Dispose() => dbUji.Dispose();

    [Fact]
    public void TC01_LoginPasswordSalah_Ditolak()
    {
        var ex = Assert.Throws<AturanException>(() => AuthService.Login("koordinator", "salah"));
        Assert.Equal("Username atau password salah.", ex.Message);
        Assert.Null(Sesi.Aktif);

        DbUji.MasukSebagai("koordinator");
        Assert.Equal(Peran.Koordinator, Sesi.Aktif!.Role);
    }

    [Fact]
    public void TC02_NrpGanda_Ditolak()
    {
        DbUji.MasukSebagai("koordinator");
        var ex = Assert.Throws<AturanException>(() =>
            PraktikanService.Simpan(new InputPraktikan(null, "5025241001", "Orang Lain", "2024", "")));
        Assert.Equal("NRP sudah terdaftar.", ex.Message);
    }

    [Fact]
    public void BR08_NrpHarus10Digit()
    {
        DbUji.MasukSebagai("koordinator");
        Assert.Throws<AturanException>(() => PraktikanService.Simpan(new InputPraktikan(null, "123456789", "A", "2024", "")));
        Assert.Throws<AturanException>(() => PraktikanService.Simpan(new InputPraktikan(null, "50252410ab", "A", "2024", "")));
    }

    [Fact]
    public void TC03_Nilai105_DitolakDanTidakTersimpan()
    {
        DbUji.MasukSebagai("asisten1");
        var kelasA = DbUji.IdKelas("A");
        var modul6 = DbUji.IdModul(6);
        var praktikan = DbUji.IdPraktikan("5025241001");

        Assert.Throws<AturanException>(() =>
            NilaiService.Simpan(kelasA, modul6, [DbUji.Skor(praktikan, 105, 80, 80)]));

        Assert.All(NilaiService.Ambil(kelasA, modul6).Baris, b => Assert.Null(b.NilaiModul));
    }

    [Fact]
    public void TC04_SimpanNilai_NilaiModulDihitungOtomatis()
    {
        DbUji.MasukSebagai("asisten1");
        var kelasA = DbUji.IdKelas("A");
        var modul6 = DbUji.IdModul(6);
        var praktikan = DbUji.IdPraktikan("5025241001");

        NilaiService.Simpan(kelasA, modul6, [DbUji.Skor(praktikan, 80, 90, 70)]);

        var baris = NilaiService.Ambil(kelasA, modul6).Baris.Single(b => b.PraktikanId == praktikan);
        Assert.Equal(82.00m, baris.NilaiModul);
    }

    [Fact]
    public void T4_PerubahanNilaiTercatatDiLog()
    {
        DbUji.MasukSebagai("asisten1");
        var kelasA = DbUji.IdKelas("A");
        var modul6 = DbUji.IdModul(6);
        var praktikan = DbUji.IdPraktikan("5025241001");

        NilaiService.Simpan(kelasA, modul6, [DbUji.Skor(praktikan, 80, 90, 70)]);
        NilaiService.Simpan(kelasA, modul6, [DbUji.Skor(praktikan, 90, 90, 70)]);

        var log = LogService.Terbaru(5);
        Assert.Equal("Ubah nilai", log[0].Aksi);
        Assert.Contains("5025241001", log[0].Detail);
        Assert.Equal("Input nilai", log[1].Aksi);
    }

    [Fact]
    public void TC05_AsistenTidakBisaMembukaKelasLain()
    {
        DbUji.MasukSebagai("asisten1"); // ditugaskan di Kelas A saja
        var kelasB = DbUji.IdKelas("B");

        Assert.Equal(["PSD · Kelas A"], KelasService.UntukPengguna().Select(k => k.Label));
        Assert.Throws<AturanException>(() => RekapService.Hitung(kelasB));
        Assert.Throws<AturanException>(() => AbsensiService.Ambil(kelasB, DbUji.IdModul(1)));
        Assert.Throws<AturanException>(() =>
            NilaiService.Simpan(kelasB, DbUji.IdModul(6), [DbUji.Skor(DbUji.IdPraktikan("5025241016"), 80, 80, 80)]));
    }

    [Fact]
    public void TC05_DosenHanyaMelihatPraktikumYangDiampu()
    {
        DbUji.MasukSebagai("koordinator");
        var praktikumLain = PraktikumService.Simpan(new InputMataPraktikum(null, "PBO", "Praktikum PBO", "Ganjil", "2026/2027", null));
        var kelasLain = KelasService.Simpan(new InputKelas(null, praktikumLain, "A", "Kamis", "", ""));

        DbUji.MasukSebagai("dosen1");
        Assert.DoesNotContain(kelasLain, KelasService.UntukPengguna().Select(k => k.Id));
        Assert.Throws<AturanException>(() => RekapService.Hitung(kelasLain));
        Assert.Throws<AturanException>(() => PraktikanService.Semua()); // halaman koordinator
    }

    [Fact]
    public void Dosen_HanyaBaca()
    {
        DbUji.MasukSebagai("dosen1");
        var ex = Assert.Throws<AturanException>(() =>
            AbsensiService.Simpan(DbUji.IdKelas("A"), DbUji.IdModul(1), []));
        Assert.Equal("Dosen hanya memiliki akses baca.", ex.Message);
    }

    [Fact]
    public void TC07_KelasTerkunci_NilaiDanAbsensiTidakBisaDiubah()
    {
        var kelasA = DbUji.IdKelas("A");
        DbUji.MasukSebagai("koordinator");
        KunciService.Kunci(kelasA);

        DbUji.MasukSebagai("asisten1");
        var praktikan = DbUji.IdPraktikan("5025241001");
        Assert.True(NilaiService.Ambil(kelasA, DbUji.IdModul(1)).Terkunci);
        Assert.Throws<AturanException>(() =>
            NilaiService.Simpan(kelasA, DbUji.IdModul(1), [DbUji.Skor(praktikan, 10, 10, 10)]));
        Assert.Throws<AturanException>(() =>
            AbsensiService.Simpan(kelasA, DbUji.IdModul(1), [new InputAbsensi(praktikan, StatusAbsensi.Alpa)]));

        // Setelah koordinator membuka kunci, asisten bisa mengubah lagi.
        DbUji.MasukSebagai("koordinator");
        KunciService.BukaKunci(kelasA);
        DbUji.MasukSebagai("asisten1");
        Assert.Equal(1, AbsensiService.Simpan(kelasA, DbUji.IdModul(1), [new InputAbsensi(praktikan, StatusAbsensi.Alpa)]));
    }

    [Fact]
    public void Kunci_HanyaKoordinator()
    {
        DbUji.MasukSebagai("asisten1");
        Assert.Throws<AturanException>(() => KunciService.Kunci(DbUji.IdKelas("A")));
    }

    [Fact]
    public void TC08_ExportExcelSesuaiRekap()
    {
        DbUji.MasukSebagai("koordinator");
        var rekap = RekapService.Hitung(DbUji.IdKelas("A"));
        var fileExcel = Path.Combine(Path.GetTempPath(), $"aslab-uji-{Guid.NewGuid()}.xlsx");
        var filePdf = Path.ChangeExtension(fileExcel, ".pdf");

        try
        {
            ExportService.KeExcel(rekap, fileExcel);
            ExportService.KePdf(rekap, filePdf);

            using var workbook = new XLWorkbook(fileExcel);
            var sheet = workbook.Worksheet(1);
            var kolom = ExportService.Kolom(rekap);
            for (var c = 0; c < kolom.Count; c++)
                Assert.Equal(kolom[c], sheet.Cell(6, c + 1).GetString());
            for (var r = 0; r < rekap.Baris.Count; r++)
            {
                Assert.Equal(rekap.Baris[r].Nrp, sheet.Cell(7 + r, 2).GetString());
                Assert.Equal(rekap.Baris[r].Status, sheet.Cell(7 + r, kolom.Count).GetString());
            }
            Assert.True(sheet.Cell(7 + rekap.Baris.Count, 2).IsEmpty());
            Assert.True(new FileInfo(filePdf).Length > 1000);
        }
        finally
        {
            File.Delete(fileExcel);
            File.Delete(filePdf);
        }
    }

    [Fact]
    public void TC12_UbahBobot_NilaiDihitungUlang()
    {
        DbUji.MasukSebagai("asisten1");
        var kelasA = DbUji.IdKelas("A");
        var modul6 = DbUji.IdModul(6);
        var praktikan = DbUji.IdPraktikan("5025241001");
        NilaiService.Simpan(kelasA, modul6, [DbUji.Skor(praktikan, 80, 90, 70)]);

        DbUji.MasukSebagai("koordinator");
        int praktikumId;
        using (var db = Db.Buka()) praktikumId = db.MataPraktikum.Single().Id;
        var k = PengaturanService.AmbilKomponen(praktikumId); // Pretest, Praktik, Laporan
        PengaturanService.SimpanKomponen(praktikumId,
        [
            new InputKomponen(k[0].Id, "Pretest", 30),
            new InputKomponen(k[1].Id, "Praktik", 40),
            new InputKomponen(k[2].Id, "Laporan", 30),
        ]);

        var baris = NilaiService.Ambil(kelasA, modul6).Baris.Single(b => b.PraktikanId == praktikan);
        Assert.Equal(81.00m, baris.NilaiModul);

        // Ditolak bila ada kelas yang sudah dikunci (BR-13).
        KunciService.Kunci(kelasA);
        Assert.Throws<AturanException>(() => PengaturanService.SimpanKomponen(praktikumId,
            k.Select(x => new InputKomponen(x.Id, x.Nama, x.Bobot)).ToList()));
    }

    [Fact]
    public void BR13_SetelahAdaNilai_KomponenTidakBisaDitambahAtauDihapus_TapiBisaDiganti()
    {
        DbUji.MasukSebagai("koordinator");
        int praktikumId;
        using (var db = Db.Buka()) praktikumId = db.MataPraktikum.Single().Id;
        var k = PengaturanService.AmbilKomponen(praktikumId);

        // Data contoh sudah berisi nilai.
        Assert.True(PengaturanService.SudahAdaNilai(praktikumId));
        Assert.Throws<AturanException>(() => PengaturanService.SimpanKomponen(praktikumId,
        [
            new InputKomponen(k[0].Id, "Pretest", 20),
            new InputKomponen(k[1].Id, "Praktik", 40),
            new InputKomponen(k[2].Id, "Laporan", 30),
            new InputKomponen(null, "Demo", 10),
        ]));
        Assert.Throws<AturanException>(() => PengaturanService.SimpanKomponen(praktikumId,
        [
            new InputKomponen(k[1].Id, "Praktik", 70),
            new InputKomponen(k[2].Id, "Laporan", 30),
        ]));

        // Mengganti nama tetap boleh.
        PengaturanService.SimpanKomponen(praktikumId,
        [
            new InputKomponen(k[0].Id, "Tes Awal", 20),
            new InputKomponen(k[1].Id, "Praktik", 50),
            new InputKomponen(k[2].Id, "Laporan", 30),
        ]);
        Assert.Equal("Tes Awal", PengaturanService.AmbilKomponen(praktikumId)[0].Nama);
    }

    [Fact]
    public void KomponenKustom_PraktikumBaru_NilaiModulSesuaiBobotnya()
    {
        DbUji.MasukSebagai("koordinator");
        var praktikumId = PraktikumService.Simpan(new InputMataPraktikum(null, "PBO", "Praktikum PBO", "Ganjil", "2026/2027", null));

        // Praktikum baru otomatis memakai komponen bawaan.
        Assert.Equal(["Pretest", "Praktik", "Laporan"], PengaturanService.AmbilKomponen(praktikumId).Select(x => x.Nama));

        // Ganti menjadi 4 komponen: Kuis 10, Tugas 20, Demo 30, Laporan 40.
        PengaturanService.SimpanKomponen(praktikumId,
        [
            new InputKomponen(null, "Kuis", 10),
            new InputKomponen(null, "Tugas", 20),
            new InputKomponen(null, "Demo", 30),
            new InputKomponen(null, "Laporan", 40),
        ]);
        var komponen = PengaturanService.AmbilKomponen(praktikumId);
        Assert.Equal(["Kuis", "Tugas", "Demo", "Laporan"], komponen.Select(x => x.Nama));

        var kelasId = KelasService.Simpan(new InputKelas(null, praktikumId, "A", "Kamis", "", ""));
        var modulId = PraktikumService.SimpanModul(new InputModul(null, praktikumId, "1", "OOP Dasar"));
        var praktikan = DbUji.IdPraktikan("5025241001");
        KelasService.TambahPeserta(kelasId, praktikan);

        var skor = new Dictionary<int, decimal>
        {
            [komponen[0].Id] = 100, [komponen[1].Id] = 80, [komponen[2].Id] = 60, [komponen[3].Id] = 90,
        };
        NilaiService.Simpan(kelasId, modulId, [new InputNilai(praktikan, skor)]);

        // (100×10 + 80×20 + 60×30 + 90×40) ÷ 100 = 80
        var lembar = NilaiService.Ambil(kelasId, modulId);
        Assert.Equal(4, lembar.Komponen.Count);
        Assert.Equal(80.00m, lembar.Baris.Single().NilaiModul);
        Assert.Equal([100m, 80m, 60m, 90m], lembar.Baris.Single().Skor.Select(s => s!.Value));
    }

    [Fact]
    public void Komponen_TotalBobotHarus100_NamaTidakBolehKembar()
    {
        DbUji.MasukSebagai("koordinator");
        var praktikumId = PraktikumService.Simpan(new InputMataPraktikum(null, "PBO", "Praktikum PBO", "Ganjil", "2026/2027", null));

        Assert.Throws<AturanException>(() => PengaturanService.SimpanKomponen(praktikumId,
            [new InputKomponen(null, "Kuis", 50), new InputKomponen(null, "Tugas", 40)]));
        Assert.Throws<AturanException>(() => PengaturanService.SimpanKomponen(praktikumId,
            [new InputKomponen(null, "Kuis", 50), new InputKomponen(null, "kuis", 50)]));
        Assert.Throws<AturanException>(() => PengaturanService.SimpanKomponen(praktikumId, []));
    }

    [Fact]
    public void SimpanNilai_DenganKomponenLama_Ditolak()
    {
        DbUji.MasukSebagai("asisten1");
        var input = new InputNilai(DbUji.IdPraktikan("5025241001"), new Dictionary<int, decimal> { [999] = 80 });
        var ex = Assert.Throws<AturanException>(() => NilaiService.Simpan(DbUji.IdKelas("A"), DbUji.IdModul(6), [input]));
        Assert.Contains("Muat ulang", ex.Message);
    }

    [Fact]
    public void TC13_ImportCsv_BarisValidMasuk_BarisSalahDilaporkan()
    {
        DbUji.MasukSebagai("koordinator");
        int praktikumId;
        using (var db = Db.Buka()) praktikumId = db.MataPraktikum.Single().Id;

        const string csv = """
            nrp,nama,angkatan,email,nama_kelas
            5025249901,Praktikan Baru,2024,baru@mhs.kampus.ac.id,B
            502524990,NRP Sembilan Digit,2024,,
            5025241001,NRP Sudah Ada,2024,,
            """;
        var hasil = ImportCsvService.Import(new StringReader(csv), praktikumId);

        Assert.Equal(1, hasil.Berhasil);
        Assert.Equal(2, hasil.Gagal.Count);
        Assert.StartsWith("Baris 3:", hasil.Gagal[0]);
        Assert.Contains("NRP sudah terdaftar", hasil.Gagal[1]);
        Assert.Contains(KelasService.Peserta(DbUji.IdKelas("B")), p => p.Nrp == "5025249901");
    }

    [Fact]
    public void BR14_PraktikanHanyaSatuKelasPerPraktikum()
    {
        DbUji.MasukSebagai("koordinator");
        var ex = Assert.Throws<AturanException>(() =>
            KelasService.TambahPeserta(DbUji.IdKelas("B"), DbUji.IdPraktikan("5025241001")));
        Assert.Contains("Kelas A", ex.Message);
    }

    [Fact]
    public void Dashboard_MenampilkanPraktikanBerisiko_SesuaiCakupanRole()
    {
        DbUji.MasukSebagai("koordinator");
        var semua = DashboardService.Ambil();
        Assert.Contains(semua.Berisiko, b => b.Nrp == "5025241004"); // sengaja sering alpa di data contoh
        Assert.Equal("5 / 8", $"{semua.ModulBerjalan} / {semua.TotalModul}");

        DbUji.MasukSebagai("asisten2"); // hanya Kelas B
        var kelasB = DashboardService.Ambil();
        Assert.All(kelasB.Berisiko, b => Assert.Equal("PSD · Kelas B", b.Kelas));
        Assert.Equal(15, kelasB.TotalPraktikan);
    }

    [Fact]
    public void DetailPraktikan_AsistenTidakBisaMelihatPraktikanKelasLain()
    {
        DbUji.MasukSebagai("asisten1");
        Assert.Throws<AturanException>(() => DetailPraktikanService.Ambil(DbUji.IdPraktikan("5025241016")));
        Assert.Single(DetailPraktikanService.Ambil(DbUji.IdPraktikan("5025241001")).DaftarKelas);
    }

    [Fact]
    public void Backup_MembuatSalinanDatabase()
    {
        DbUji.MasukSebagai("koordinator");
        // File .bak ditulis oleh service SQL Server, jadi pakai folder Public yang bisa diaksesnya.
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), $"aslab-backup-uji-{Guid.NewGuid()}.bak");
        try
        {
            PengaturanService.Backup(file);
            Assert.True(new FileInfo(file).Length > 0);
        }
        finally
        {
            File.Delete(file);
        }
    }

    [Fact]
    public void Akun_TidakBisaMenghapusDiriSendiri()
    {
        DbUji.MasukSebagai("koordinator");
        Assert.Throws<AturanException>(() => AkunService.Hapus(Sesi.Wajib.Id));
    }
}
