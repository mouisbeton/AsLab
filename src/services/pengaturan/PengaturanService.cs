using System.IO;
using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Pengaturan;

/// <summary>Komponen yang diisi di halaman Pengaturan. Id null berarti komponen baru.</summary>
public record InputKomponen(int? Id, string Nama, decimal Bobot);

/// <summary>Pengaturan komponen nilai (FR-19, BR-01, BR-13) dan backup database (FR-18). Hanya koordinator.</summary>
public static class PengaturanService
{
    public static List<InfoKomponen> AmbilKomponen(int praktikumId)
    {
        using var db = Db.Buka();
        return NilaiService.Komponen(db, praktikumId);
    }

    /// <summary>Apakah sudah ada nilai yang diinput untuk mata praktikum ini.</summary>
    public static bool SudahAdaNilai(int praktikumId)
    {
        using var db = Db.Buka();
        return db.NilaiKomponen.Any(s => s.Komponen.PraktikumId == praktikumId);
    }

    /// <summary>
    /// Menyimpan daftar komponen nilai (urutan daftar = urutan kolom di Input Nilai), lalu menghitung
    /// ulang semua nilai modul. Mengembalikan jumlah nilai yang dihitung ulang.
    /// </summary>
    public static int SimpanKomponen(int praktikumId, List<InputKomponen> daftar)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);
        daftar = daftar.Select(k => k with { Nama = k.Nama.Trim() }).ToList();
        var error = Perhitungan.ValidasiKomponenPraktikum(daftar.Select(k => (k.Nama, k.Bobot)).ToList());
        if (error != null) throw new AturanException(error);

        using var db = Db.Buka();
        using var transaksi = db.Database.BeginTransaction();

        var praktikum = db.MataPraktikum.Find(praktikumId) ?? throw new AturanException("Mata praktikum tidak ditemukan.");
        if (db.Kelas.Any(k => k.PraktikumId == praktikumId && k.IsLocked))
            throw new AturanException("Komponen nilai tidak dapat diubah karena sudah ada kelas yang dikunci. Buka kunci semua kelas terlebih dahulu.");

        var komponenLama = db.KomponenNilai.Where(k => k.PraktikumId == praktikumId).OrderBy(k => k.Urutan).ToList();
        var teksLama = string.Join(", ", komponenLama.Select(k => new InfoKomponen(k.Id, k.Nama, k.Bobot).Teks));
        var idDipertahankan = daftar.Where(k => k.Id != null).Select(k => k.Id!.Value).ToList();
        if (idDipertahankan.Any(id => komponenLama.All(k => k.Id != id)))
            throw new AturanException("Komponen tidak ditemukan. Muat ulang halaman.");

        // BR-13: setelah ada nilai, komponen tidak boleh ditambah/dihapus (nilai lama akan jadi tidak lengkap).
        var sudahAdaNilai = db.NilaiKomponen.Any(s => s.Komponen.PraktikumId == praktikumId);
        var adaYangDihapus = komponenLama.Any(k => !idDipertahankan.Contains(k.Id));
        var adaYangBaru = daftar.Any(k => k.Id == null);
        if (sudahAdaNilai && (adaYangDihapus || adaYangBaru))
            throw new AturanException("Sudah ada nilai yang diinput, jadi komponen tidak dapat ditambah atau dihapus. Nama dan bobot tetap bisa diubah.");

        db.KomponenNilai.RemoveRange(komponenLama.Where(k => !idDipertahankan.Contains(k.Id)));
        for (var i = 0; i < daftar.Count; i++)
        {
            var input = daftar[i];
            var komponen = komponenLama.FirstOrDefault(k => k.Id == input.Id);
            if (komponen == null)
            {
                komponen = new KomponenNilai { PraktikumId = praktikumId };
                db.KomponenNilai.Add(komponen);
            }
            komponen.Nama = input.Nama;
            komponen.Bobot = input.Bobot;
            komponen.Urutan = i + 1;
        }

        // Hitung ulang nilai modul seluruh praktikan pada mata praktikum ini dengan bobot baru.
        var bobotBaru = daftar.Where(k => k.Id != null).ToDictionary(k => k.Id!.Value, k => k.Bobot);
        var semuaNilai = db.Nilai.Include(n => n.DaftarSkor).Where(n => n.Modul.PraktikumId == praktikumId).ToList();
        foreach (var nilai in semuaNilai)
            nilai.NilaiModul = Perhitungan.NilaiModul(nilai.DaftarSkor.Select(s => (s.Skor, bobotBaru[s.KomponenId])));

        var teksBaru = string.Join(", ", daftar.Select(k => new InfoKomponen(0, k.Nama, k.Bobot).Teks));
        LogService.Catat(db, pengguna.Id, "Ubah komponen nilai",
            $"{praktikum.Kode}: {teksLama} → {teksBaru} ({semuaNilai.Count} nilai dihitung ulang)");
        db.SaveChanges();
        transaksi.Commit();
        return semuaNilai.Count;
    }

    /// <summary>
    /// Membuat file backup (.bak) memakai perintah BACKUP DATABASE milik SQL Server.
    /// File .bak bisa dipulihkan lewat SSMS: klik kanan Databases → Restore Database.
    /// </summary>
    public static void Backup(string fileTujuan)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        LogService.Catat(db, pengguna.Id, "Backup database", Path.GetFileName(fileTujuan));
        db.SaveChanges();

        var namaDatabase = db.Database.GetDbConnection().Database;
        try
        {
            // Query berparameter: nama database dan lokasi file dikirim sebagai parameter.
            // WITH INIT menimpa isi file bila file sudah ada.
            db.Database.ExecuteSqlRaw("BACKUP DATABASE @p0 TO DISK = @p1 WITH INIT", namaDatabase, fileTujuan);
        }
        catch (SqlException ex) when (ex.Number is 3201 or 3013)
        {
            // File ditulis oleh service SQL Server, bukan oleh akun Windows Anda.
            throw new AturanException(
                "SQL Server tidak dapat menulis ke folder tersebut. Pilih folder yang bisa diakses service SQL Server, " +
                "misalnya C:\\Users\\Public\\Documents, lalu coba lagi.");
        }
    }
}
