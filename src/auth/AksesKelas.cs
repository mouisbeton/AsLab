using AsLab.Data;
using AsLab.Services;

namespace AsLab.Auth;

/// <summary>
/// Pembatasan akses per role (BR-09, BR-11). Semua service memanggil fungsi di sini
/// sebelum membaca atau mengubah data kelas.
/// </summary>
public static class AksesKelas
{
    /// <summary>Daftar id kelas yang boleh diakses pengguna.</summary>
    public static List<int> KelasDiizinkan(AsLabDbContext db, Pengguna pengguna)
    {
        if (pengguna.IsKoordinator)
            return db.Kelas.Select(k => k.Id).ToList();

        if (pengguna.IsAsisten)
            return db.PenugasanAsisten.Where(p => p.AsistenId == pengguna.Id).Select(p => p.KelasId).ToList();

        // Dosen: semua kelas dari mata praktikum yang diampunya.
        return db.Kelas.Where(k => k.Praktikum.DosenId == pengguna.Id).Select(k => k.Id).ToList();
    }

    public static void PastikanBoleh(AsLabDbContext db, Pengguna pengguna, int kelasId)
    {
        if (!KelasDiizinkan(db, pengguna).Contains(kelasId))
            throw new AturanException("Anda tidak memiliki akses ke kelas ini.");
    }

    public static void PastikanKoordinator(Pengguna pengguna)
    {
        if (!pengguna.IsKoordinator)
            throw new AturanException("Hanya koordinator yang dapat melakukan aksi ini.");
    }

    /// <summary>Dosen hanya memiliki akses baca.</summary>
    public static void PastikanBolehMengubah(Pengguna pengguna)
    {
        if (pengguna.IsDosen)
            throw new AturanException("Dosen hanya memiliki akses baca.");
    }
}
