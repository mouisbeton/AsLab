using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Master;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Penilaian;

public record BarisAbsensi(int PraktikanId, string Nrp, string Nama, string? Status);

public record InputAbsensi(int PraktikanId, string Status);

/// <summary>Input absensi per kelas dan modul (FR-06).</summary>
public static class AbsensiService
{
    public static List<BarisAbsensi> Ambil(int kelasId, int modulId)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        AksesKelas.PastikanBoleh(db, pengguna, kelasId);

        var statusTersimpan = db.Absensi.AsNoTracking()
            .Where(a => a.KelasId == kelasId && a.ModulId == modulId)
            .ToDictionary(a => a.PraktikanId, a => a.Status);

        return db.PesertaKelas.AsNoTracking()
            .Where(p => p.KelasId == kelasId)
            .Select(p => p.Praktikan)
            .OrderBy(p => p.Nrp)
            .ToList()
            .Select(p => new BarisAbsensi(p.Id, p.Nrp, p.Nama, statusTersimpan.GetValueOrDefault(p.Id)))
            .ToList();
    }

    /// <summary>Menyimpan status absensi. Mengembalikan jumlah status yang berubah.</summary>
    public static int Simpan(int kelasId, int modulId, List<InputAbsensi> data)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        using var transaksi = db.Database.BeginTransaction();

        var kelas = RekapService.KelasBolehDiubah(db, pengguna, kelasId);
        var modul = db.Modul.FirstOrDefault(m => m.Id == modulId && m.PraktikumId == kelas.PraktikumId)
            ?? throw new AturanException("Modul tidak ditemukan pada mata praktikum kelas ini.");
        var pesertaKelas = db.PesertaKelas.Where(p => p.KelasId == kelasId).Select(p => p.PraktikanId).ToList();
        var absensiLama = db.Absensi.Where(a => a.KelasId == kelasId && a.ModulId == modulId).ToList();

        var jumlahBerubah = 0;
        foreach (var input in data)
        {
            if (!StatusAbsensi.Semua.Contains(input.Status))
                throw new AturanException($"Status \"{input.Status}\" tidak dikenal.");
            if (!pesertaKelas.Contains(input.PraktikanId))
                throw new AturanException("Ada praktikan yang bukan peserta kelas ini.");

            var lama = absensiLama.FirstOrDefault(a => a.PraktikanId == input.PraktikanId);
            if (lama == null)
            {
                db.Absensi.Add(new Absensi
                {
                    ModulId = modulId, KelasId = kelasId, PraktikanId = input.PraktikanId,
                    Status = input.Status, DicatatOleh = pengguna.Id, Waktu = DateTime.Now,
                });
                jumlahBerubah++;
            }
            else if (lama.Status != input.Status)
            {
                lama.Status = input.Status;
                lama.DicatatOleh = pengguna.Id;
                lama.Waktu = DateTime.Now;
                jumlahBerubah++;
            }
        }

        if (jumlahBerubah > 0)
        {
            var ringkasan = string.Join(", ", StatusAbsensi.Semua.Select(s => $"{s} {data.Count(d => d.Status == s)}"));
            LogService.Catat(db, pengguna.Id, "Input absensi",
                $"{Format.NamaKelas(kelas.Praktikum.Kode, kelas.NamaKelas)} · M{modul.Nomor}: {jumlahBerubah} status disimpan ({ringkasan})");
            db.SaveChanges();
        }
        transaksi.Commit();
        return jumlahBerubah;
    }
}
