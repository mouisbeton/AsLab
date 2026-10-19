using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Penilaian;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

// Sesi dan Db bersifat statis, jadi tes dijalankan satu per satu.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace AsLab.Tests;

/// <summary>
/// Database SQL Server khusus tes (AsLab_Uji, terpisah dari database aplikasi) berisi data contoh.
/// Database dibuat ulang sekali per run, lalu setiap tes mengosongkan semua tabel dan mengisi ulang
/// data contoh, sehingga setiap tes mulai dari data yang sama.
/// </summary>
public sealed class DbUji : IDisposable
{
    private static bool databaseSudahDibuat;

    // Tabel anak dihapus lebih dulu agar foreign key tidak menolak.
    private static readonly string[] UrutanHapus =
    [
        "nilai_komponen", "nilai", "absensi", "log_aktivitas", "jadwal_modul", "peserta_kelas",
        "penugasan_asisten", "komponen_nilai", "kelas", "modul", "mata_praktikum", "praktikan", "users",
    ];

    /// <summary>Connection string aplikasi dengan nama database diganti AsLab_Uji (atau ASLAB_KONEKSI_UJI bila diisi).</summary>
    public static string KoneksiUji() =>
        Environment.GetEnvironmentVariable("ASLAB_KONEKSI_UJI")
        ?? new SqlConnectionStringBuilder(Konfigurasi.ConnectionString()) { InitialCatalog = "AsLab_Uji" }.ConnectionString;

    public DbUji()
    {
        AuthService.WorkFactor = 4; // hash cepat agar tes tidak lambat
        Db.GunakanSqlServer(KoneksiUji());

        using var db = Db.Buka();
        if (!databaseSudahDibuat)
        {
            db.Database.EnsureDeleted();
            db.Database.Migrate();
            databaseSudahDibuat = true;
        }
        else
        {
            db.Database.ExecuteSqlRaw(string.Join(";", UrutanHapus.Select(t => $"DELETE FROM [{t}]")));
        }

        Seeder.Isi(db);
        Sesi.Keluar();
    }

    /// <summary>Login memakai akun contoh (password = username + "123").</summary>
    public static void MasukSebagai(string username) => AuthService.Login(username, username + "123");

    public static int IdKelas(string namaKelas)
    {
        using var db = Db.Buka();
        return db.Kelas.Single(k => k.NamaKelas == namaKelas).Id;
    }

    public static int IdModul(int nomor)
    {
        using var db = Db.Buka();
        return db.Modul.Single(m => m.Nomor == nomor).Id;
    }

    public static int IdPraktikan(string nrp)
    {
        using var db = Db.Buka();
        return db.Praktikan.Single(p => p.Nrp == nrp).Id;
    }

    /// <summary>
    /// Membuat InputNilai untuk mata praktikum contoh (PSD), skor diisi sesuai urutan komponen.
    /// Contoh: Skor(id, 80, 90, 70) = Pretest 80, Praktik 90, Laporan 70.
    /// </summary>
    public static InputNilai Skor(int praktikanId, params decimal[] skor)
    {
        using var db = Db.Buka();
        var komponen = NilaiService.Komponen(db, db.MataPraktikum.Single(p => p.Kode == "PSD").Id);
        return new InputNilai(praktikanId, komponen.Select((k, i) => (k.Id, skor[i])).ToDictionary(x => x.Id, x => x.Item2));
    }

    public void Dispose()
    {
        Sesi.Keluar();
    }
}
