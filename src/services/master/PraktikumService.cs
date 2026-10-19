using System.Text.RegularExpressions;
using AsLab.Auth;
using AsLab.Data;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Master;

public record BarisMataPraktikum(int Id, string Kode, string Nama, string Semester, string TahunAjaran, int? DosenId, string NamaDosen)
{
    public string Label => $"{Kode} – {Nama}";
}

public record InputMataPraktikum(int? Id, string Kode, string Nama, string Semester, string TahunAjaran, int? DosenId);

public record BarisModul(int Id, int Nomor, string Judul)
{
    public string Label => $"M{Nomor} · {Judul}";
}

public record InputModul(int? Id, int PraktikumId, string Nomor, string Judul);

/// <summary>CRUD mata praktikum dan modul (FR-04). Hanya koordinator.</summary>
public static class PraktikumService
{
    public static readonly string[] DaftarSemester = ["Ganjil", "Genap"];

    public static List<BarisMataPraktikum> Semua()
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        return db.MataPraktikum.AsNoTracking().Include(p => p.Dosen).OrderBy(p => p.Kode).ToList()
            .Select(p => new BarisMataPraktikum(p.Id, p.Kode, p.Nama, p.Semester, p.TahunAjaran, p.DosenId, p.Dosen?.Nama ?? "–"))
            .ToList();
    }

    public static int Simpan(InputMataPraktikum input)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        var kode = input.Kode.Trim().ToUpperInvariant();
        var nama = input.Nama.Trim();
        var tahun = input.TahunAjaran.Trim();
        if (kode == "") throw new AturanException("Kode wajib diisi, contoh PSD.");
        if (nama == "") throw new AturanException("Nama mata praktikum wajib diisi.");
        if (!DaftarSemester.Contains(input.Semester)) throw new AturanException("Pilih semester Ganjil atau Genap.");
        if (!Regex.IsMatch(tahun, @"^\d{4}/\d{4}$")) throw new AturanException("Tahun ajaran harus berformat 2026/2027.");

        using var db = Db.Buka();
        if (input.DosenId != null && !db.Users.Any(u => u.Id == input.DosenId && u.Role == Peran.Dosen))
            throw new AturanException("Dosen pengampu harus akun dengan peran dosen.");

        MataPraktikum praktikum;
        if (input.Id == null)
        {
            // Mata praktikum baru langsung memakai komponen bawaan; bisa diubah di Pengaturan.
            praktikum = new MataPraktikum { DaftarKomponen = KomponenNilai.Bawaan() };
            db.MataPraktikum.Add(praktikum);
        }
        else
        {
            praktikum = db.MataPraktikum.Find(input.Id.Value) ?? throw new AturanException("Mata praktikum tidak ditemukan.");
        }

        praktikum.Kode = kode;
        praktikum.Nama = nama;
        praktikum.Semester = input.Semester;
        praktikum.TahunAjaran = tahun;
        praktikum.DosenId = input.DosenId;
        LogService.Catat(db, pengguna.Id, input.Id == null ? "Tambah mata praktikum" : "Ubah mata praktikum", $"{kode} – {nama}");
        db.SaveChanges();
        return praktikum.Id;
    }

    public static void Hapus(int id)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var praktikum = db.MataPraktikum.Find(id) ?? throw new AturanException("Mata praktikum tidak ditemukan.");
        if (db.Kelas.Any(k => k.PraktikumId == id) || db.Modul.Any(m => m.PraktikumId == id))
            throw new AturanException("Hapus semua kelas dan modul mata praktikum ini terlebih dahulu.");

        db.MataPraktikum.Remove(praktikum);
        LogService.Catat(db, pengguna.Id, "Hapus mata praktikum", $"{praktikum.Kode} – {praktikum.Nama}");
        db.SaveChanges();
    }

    public static List<BarisModul> DaftarModul(int praktikumId)
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        return db.Modul.AsNoTracking().Where(m => m.PraktikumId == praktikumId).OrderBy(m => m.Nomor)
            .Select(m => new BarisModul(m.Id, m.Nomor, m.Judul)).ToList();
    }

    public static int SimpanModul(InputModul input)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        var judul = input.Judul.Trim();
        if (!int.TryParse(input.Nomor.Trim(), out var nomor) || nomor < 1)
            throw new AturanException("Nomor modul harus angka mulai dari 1.");
        if (judul == "") throw new AturanException("Judul modul wajib diisi.");

        using var db = Db.Buka();
        if (db.Modul.Any(m => m.PraktikumId == input.PraktikumId && m.Nomor == nomor && m.Id != input.Id))
            throw new AturanException($"Modul nomor {nomor} sudah ada di mata praktikum ini.");

        Modul modul;
        if (input.Id == null)
        {
            modul = new Modul { PraktikumId = input.PraktikumId };
            db.Modul.Add(modul);
        }
        else
        {
            modul = db.Modul.Find(input.Id.Value) ?? throw new AturanException("Modul tidak ditemukan.");
        }

        modul.Nomor = nomor;
        modul.Judul = judul;
        LogService.Catat(db, pengguna.Id, input.Id == null ? "Tambah modul" : "Ubah modul", $"M{nomor} · {judul}");
        db.SaveChanges();
        return modul.Id;
    }

    public static void HapusModul(int id)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var modul = db.Modul.Find(id) ?? throw new AturanException("Modul tidak ditemukan.");
        if (db.Absensi.Any(a => a.ModulId == id) || db.Nilai.Any(n => n.ModulId == id))
            throw new AturanException("Modul sudah memiliki data absensi atau nilai sehingga tidak dapat dihapus.");

        db.Modul.Remove(modul);
        LogService.Catat(db, pengguna.Id, "Hapus modul", $"M{modul.Nomor} · {modul.Judul}");
        db.SaveChanges();
    }
}
