using System.Text.RegularExpressions;
using AsLab.Auth;
using AsLab.Data;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Master;

public record BarisAkun(int Id, string Username, string Nama, string Role, string KelasDitugaskan)
{
    public string PeranLabel => Peran.Label(Role);
}

/// <summary>Password kosong saat mengubah akun berarti password tidak diganti.</summary>
public record InputAkun(int? Id, string Username, string Nama, string Role, string Password);

public record OpsiPengguna(int Id, string Nama)
{
    public override string ToString() => Nama;
}

/// <summary>Kelola akun dan penugasan asisten ke kelas (FR-05). Hanya koordinator.</summary>
public static class AkunService
{
    public static List<BarisAkun> Semua()
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();

        var penugasan = db.PenugasanAsisten.AsNoTracking().Include(p => p.Kelas).ThenInclude(k => k.Praktikum).ToList();
        return db.Users.AsNoTracking().OrderBy(u => u.Role).ThenBy(u => u.Username).ToList()
            .Select(u =>
            {
                var kelas = penugasan.Where(p => p.AsistenId == u.Id)
                    .Select(p => Format.NamaKelas(p.Kelas.Praktikum.Kode, p.Kelas.NamaKelas)).ToList();
                return new BarisAkun(u.Id, u.Username, u.Nama, u.Role, kelas.Count == 0 ? "–" : string.Join(", ", kelas));
            })
            .ToList();
    }

    public static List<OpsiPengguna> DaftarDosen()
    {
        using var db = Db.Buka();
        return db.Users.AsNoTracking().Where(u => u.Role == Peran.Dosen).OrderBy(u => u.Nama)
            .Select(u => new OpsiPengguna(u.Id, u.Nama)).ToList();
    }

    public static int Simpan(InputAkun input)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        var username = input.Username.Trim().ToLowerInvariant();
        var nama = input.Nama.Trim();
        if (!Regex.IsMatch(username, @"^[a-z0-9._]{3,30}$"))
            throw new AturanException("Username 3–30 karakter, hanya huruf kecil, angka, titik, atau garis bawah.");
        if (nama == "") throw new AturanException("Nama wajib diisi.");
        if (!Peran.Semua.Contains(input.Role)) throw new AturanException("Pilih peran akun.");
        if (input.Id == null && input.Password.Length < 6)
            throw new AturanException("Password minimal 6 karakter.");
        if (input.Id != null && input.Password.Length is > 0 and < 6)
            throw new AturanException("Password baru minimal 6 karakter (kosongkan bila tidak diganti).");

        using var db = Db.Buka();
        if (db.Users.Any(u => u.Username == username && u.Id != input.Id))
            throw new AturanException("Username sudah dipakai.");

        User user;
        if (input.Id == null)
        {
            user = new User();
            db.Users.Add(user);
        }
        else
        {
            user = db.Users.Find(input.Id.Value) ?? throw new AturanException("Akun tidak ditemukan.");
            if (user.Role != input.Role)
            {
                if (user.Id == pengguna.Id)
                    throw new AturanException("Anda tidak dapat mengubah peran akun sendiri.");
                if (user.Role == Peran.Dosen && db.MataPraktikum.Any(p => p.DosenId == user.Id))
                    throw new AturanException("Akun ini masih menjadi dosen pengampu. Ganti dosen mata praktikumnya terlebih dahulu.");
                if (user.Role == Peran.Asisten)
                    db.PenugasanAsisten.RemoveRange(db.PenugasanAsisten.Where(p => p.AsistenId == user.Id));
            }
        }

        user.Username = username;
        user.Nama = nama;
        user.Role = input.Role;
        if (input.Password.Length > 0)
            user.PasswordHash = AuthService.Hash(input.Password);

        LogService.Catat(db, pengguna.Id, input.Id == null ? "Tambah akun" : "Ubah akun", $"{username} ({Peran.Label(input.Role)})");
        db.SaveChanges();
        return user.Id;
    }

    public static void Hapus(int id)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);
        if (id == pengguna.Id) throw new AturanException("Anda tidak dapat menghapus akun sendiri.");

        using var db = Db.Buka();
        var user = db.Users.Find(id) ?? throw new AturanException("Akun tidak ditemukan.");
        var sudahDipakai = db.LogAktivitas.Any(l => l.UserId == id)
                           || db.Nilai.Any(n => n.DinilaiOleh == id)
                           || db.Absensi.Any(a => a.DicatatOleh == id)
                           || db.Kelas.Any(k => k.DikunciOleh == id)
                           || db.MataPraktikum.Any(p => p.DosenId == id);
        if (sudahDipakai)
            throw new AturanException("Akun ini sudah tercatat dalam data (log, nilai, absensi, atau dosen pengampu) sehingga tidak dapat dihapus.");

        db.Users.Remove(user);
        LogService.Catat(db, pengguna.Id, "Hapus akun", user.Username);
        db.SaveChanges();
    }

    public static List<int> KelasAsisten(int asistenId)
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        return db.PenugasanAsisten.Where(p => p.AsistenId == asistenId).Select(p => p.KelasId).ToList();
    }

    /// <summary>Mengganti seluruh penugasan seorang asisten dengan daftar kelas baru.</summary>
    public static void SimpanPenugasan(int asistenId, List<int> kelasIds)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var asisten = db.Users.Find(asistenId) ?? throw new AturanException("Akun tidak ditemukan.");
        if (asisten.Role != Peran.Asisten) throw new AturanException("Penugasan hanya untuk akun asisten.");

        db.PenugasanAsisten.RemoveRange(db.PenugasanAsisten.Where(p => p.AsistenId == asistenId));
        foreach (var kelasId in kelasIds.Distinct())
            db.PenugasanAsisten.Add(new PenugasanAsisten { AsistenId = asistenId, KelasId = kelasId });

        LogService.Catat(db, pengguna.Id, "Ubah penugasan asisten", $"{asisten.Username}: {kelasIds.Count} kelas");
        db.SaveChanges();
    }
}
