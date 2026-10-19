using System.Text.RegularExpressions;
using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Penilaian;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Master;

public record BarisPraktikan(int Id, string Nrp, string Nama, int Angkatan, string Email, string Kelas, List<int> KelasIds, bool Berisiko)
{
    public string StatusRisiko => Berisiko ? "Berisiko" : "Aman";
}

public record InputPraktikan(int? Id, string Nrp, string Nama, string Angkatan, string Email);

/// <summary>CRUD praktikan (FR-03) dan validasi NRP (BR-08).</summary>
public static class PraktikanService
{
    /// <summary>Semua praktikan beserta kelas dan status risikonya (untuk tabel, pencarian, dan filter).</summary>
    public static List<BarisPraktikan> Semua()
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        var hariIni = DateOnly.FromDateTime(DateTime.Today);

        var idBerisiko = new HashSet<int>();
        foreach (var kelasId in db.Kelas.Select(k => k.Id).ToList())
        {
            foreach (var baris in RekapService.Hitung(db, kelasId, hariIni).Baris)
                if (baris.Berisiko) idBerisiko.Add(baris.PraktikanId);
        }

        var namaKelas = db.Kelas.AsNoTracking().Include(k => k.Praktikum)
            .ToDictionary(k => k.Id, k => Format.NamaKelas(k.Praktikum.Kode, k.NamaKelas));

        return db.Praktikan.AsNoTracking().Include(p => p.Keanggotaan).OrderBy(p => p.Nrp).ToList()
            .Select(p =>
            {
                var kelasIds = p.Keanggotaan.Select(k => k.KelasId).ToList();
                var kelasTeks = kelasIds.Count == 0 ? "–" : string.Join(", ", kelasIds.Select(id => namaKelas[id]));
                return new BarisPraktikan(p.Id, p.Nrp, p.Nama, p.Angkatan, p.Email, kelasTeks, kelasIds, idBerisiko.Contains(p.Id));
            })
            .ToList();
    }

    /// <summary>Menambah (Id null) atau mengubah praktikan. Mengembalikan id praktikan.</summary>
    public static int Simpan(InputPraktikan input)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        var nrp = input.Nrp.Trim();
        var nama = input.Nama.Trim();
        var email = input.Email.Trim();
        var errorNrp = ValidasiNrp(nrp);
        if (errorNrp != null) throw new AturanException(errorNrp);
        if (nama == "") throw new AturanException("Nama wajib diisi.");
        if (!int.TryParse(input.Angkatan.Trim(), out var angkatan) || angkatan < 2000 || angkatan > DateTime.Today.Year + 1)
            throw new AturanException("Angkatan harus tahun 4 digit, contoh 2024.");
        if (email != "" && !EmailValid(email))
            throw new AturanException("Format email tidak valid, contoh nama@kampus.ac.id.");

        using var db = Db.Buka();
        if (db.Praktikan.Any(p => p.Nrp == nrp && p.Id != input.Id))
            throw new AturanException("NRP sudah terdaftar.");

        Praktikan praktikan;
        if (input.Id == null)
        {
            praktikan = new Praktikan();
            db.Praktikan.Add(praktikan);
        }
        else
        {
            praktikan = db.Praktikan.Find(input.Id.Value) ?? throw new AturanException("Praktikan tidak ditemukan.");
        }

        praktikan.Nrp = nrp;
        praktikan.Nama = nama;
        praktikan.Angkatan = angkatan;
        praktikan.Email = email;
        LogService.Catat(db, pengguna.Id, input.Id == null ? "Tambah praktikan" : "Ubah praktikan", $"{nrp} – {nama}");
        db.SaveChanges();
        return praktikan.Id;
    }

    /// <summary>Menghapus praktikan beserta absensi dan nilainya.</summary>
    public static void Hapus(int id)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var praktikan = db.Praktikan.Find(id) ?? throw new AturanException("Praktikan tidak ditemukan.");
        if (db.PesertaKelas.Any(p => p.PraktikanId == id && p.Kelas.IsLocked))
            throw new AturanException("Praktikan terdaftar di kelas yang sudah dikunci. Buka kunci kelas terlebih dahulu.");

        db.Praktikan.Remove(praktikan);
        LogService.Catat(db, pengguna.Id, "Hapus praktikan", $"{praktikan.Nrp} – {praktikan.Nama}");
        db.SaveChanges();
    }

    /// <summary>BR-08: NRP terdiri dari 10 digit angka.</summary>
    public static string? ValidasiNrp(string nrp) =>
        Regex.IsMatch(nrp, @"^\d{10}$") ? null : "NRP harus terdiri dari 10 digit angka, contoh 5025241001.";

    public static bool EmailValid(string email) => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
}
