using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Penilaian;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Dashboard;

public record RiwayatAbsensi(string Modul, string Tanggal, string Status);

/// <summary>Data seorang praktikan di satu kelas.</summary>
public class DetailKelas
{
    public RekapKelas Rekap { get; set; } = null!;
    public BarisRekap Baris { get; set; } = null!;
    public List<RiwayatAbsensi> Absensi { get; set; } = [];

    public string Label => Rekap.Label;

    public override string ToString() => Label;
}

public class DataDetailPraktikan
{
    public int Id { get; set; }
    public string Nrp { get; set; } = "";
    public string Nama { get; set; } = "";
    public int Angkatan { get; set; }
    public string Email { get; set; } = "";
    public List<DetailKelas> DaftarKelas { get; set; } = [];
}

/// <summary>Profil, progres nilai, dan riwayat absensi seorang praktikan (FR-16).</summary>
public static class DetailPraktikanService
{
    public static DataDetailPraktikan Ambil(int praktikanId)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        var hariIni = DateOnly.FromDateTime(DateTime.Today);

        var praktikan = db.Praktikan.AsNoTracking().Include(p => p.Keanggotaan).FirstOrDefault(p => p.Id == praktikanId)
            ?? throw new AturanException("Praktikan tidak ditemukan.");

        // BR-11: hanya kelas yang boleh diakses pengguna.
        var kelasDiizinkan = AksesKelas.KelasDiizinkan(db, pengguna);
        var kelasIds = praktikan.Keanggotaan.Select(k => k.KelasId).Where(kelasDiizinkan.Contains).ToList();
        if (kelasIds.Count == 0 && !pengguna.IsKoordinator)
            throw new AturanException("Anda tidak memiliki akses ke praktikan ini.");

        var data = new DataDetailPraktikan
        {
            Id = praktikan.Id, Nrp = praktikan.Nrp, Nama = praktikan.Nama, Angkatan = praktikan.Angkatan, Email = praktikan.Email,
        };

        foreach (var kelasId in kelasIds)
        {
            var rekap = RekapService.Hitung(db, kelasId, hariIni);
            var jadwal = db.JadwalModul.AsNoTracking().Where(j => j.KelasId == kelasId).ToList();
            var absensi = db.Absensi.AsNoTracking().Where(a => a.KelasId == kelasId && a.PraktikanId == praktikanId).ToList();

            var riwayat = new List<RiwayatAbsensi>();
            foreach (var modul in rekap.DaftarModul)
            {
                var tanggal = jadwal.FirstOrDefault(j => j.ModulId == modul.Id)?.Tanggal;
                var status = absensi.FirstOrDefault(a => a.ModulId == modul.Id)?.Status;
                var sudahLewat = tanggal.HasValue && tanggal.Value <= hariIni;
                if (status == null && !sudahLewat) continue; // modul yang belum berlangsung tidak ditampilkan
                riwayat.Add(new RiwayatAbsensi($"M{modul.Nomor} · {modul.Judul}",
                    tanggal.HasValue ? Format.Tanggal(tanggal.Value) : "–", status ?? "Belum dicatat"));
            }

            data.DaftarKelas.Add(new DetailKelas
            {
                Rekap = rekap,
                Baris = rekap.Baris.First(b => b.PraktikanId == praktikanId),
                Absensi = riwayat,
            });
        }

        return data;
    }
}
