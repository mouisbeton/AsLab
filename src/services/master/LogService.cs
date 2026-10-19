using AsLab.Auth;
using AsLab.Data;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Master;

public record BarisLog(DateTime Waktu, string NamaPengguna, string Aksi, string Detail)
{
    public string WaktuTeks => Format.Waktu(Waktu);
}

/// <summary>Log aktivitas (FR-15). Kontrak PRD 13.4: catatLog(userId, aksi, detail).</summary>
public static class LogService
{
    /// <summary>
    /// Menambahkan log ke DbContext yang sedang dipakai. Log ikut tersimpan saat
    /// SaveChanges dipanggil, jadi data dan log-nya selalu tersimpan bersama.
    /// </summary>
    public static void Catat(AsLabDbContext db, int userId, string aksi, string detail)
    {
        db.LogAktivitas.Add(new LogAktivitas { UserId = userId, Aksi = aksi, Detail = detail, Waktu = DateTime.Now });
    }

    /// <summary>Koordinator melihat semua log; role lain hanya aktivitasnya sendiri.</summary>
    public static List<BarisLog> Terbaru(int jumlah)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();

        var query = db.LogAktivitas.AsNoTracking();
        if (!pengguna.IsKoordinator)
            query = query.Where(l => l.UserId == pengguna.Id);

        return query
            .OrderByDescending(l => l.Id)
            .Take(jumlah)
            .Select(l => new BarisLog(l.Waktu, l.User.Nama, l.Aksi, l.Detail))
            .ToList();
    }
}
