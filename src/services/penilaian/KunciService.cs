using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Master;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Penilaian;

/// <summary>Finalisasi, kunci, dan buka kunci per kelas (FR-14, BR-10).</summary>
public static class KunciService
{
    public static void Kunci(int kelasId) => UbahKunci(kelasId, true);

    public static void BukaKunci(int kelasId) => UbahKunci(kelasId, false);

    private static void UbahKunci(int kelasId, bool kunci)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var kelas = db.Kelas.Include(k => k.Praktikum).FirstOrDefault(k => k.Id == kelasId)
            ?? throw new AturanException("Kelas tidak ditemukan.");
        if (kelas.IsLocked == kunci)
            throw new AturanException(kunci ? "Kelas ini sudah dikunci." : "Kelas ini belum dikunci.");

        kelas.IsLocked = kunci;
        kelas.DikunciOleh = kunci ? pengguna.Id : null;
        kelas.DikunciPada = kunci ? DateTime.Now : null;
        LogService.Catat(db, pengguna.Id, kunci ? "Kunci kelas" : "Buka kunci kelas",
            Format.NamaKelas(kelas.Praktikum.Kode, kelas.NamaKelas));
        db.SaveChanges();
    }
}
