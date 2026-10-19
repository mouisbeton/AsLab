using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Dashboard;

public record BarisRisiko(int PraktikanId, string Nrp, string Nama, string Kelas, string Alasan);

public record RataModul(string Label, decimal Rata);

public class DataDashboard
{
    public int TotalPraktikan { get; set; }
    public int KelasAktif { get; set; }
    public int ModulBerjalan { get; set; }
    public int TotalModul { get; set; }
    public decimal? RataKehadiran { get; set; }
    public List<RataModul> RataPerModul { get; set; } = [];
    public List<(string Huruf, int Jumlah)> DistribusiHuruf { get; set; } = [];
    public List<BarisRisiko> Berisiko { get; set; } = [];
    public List<BarisLog> Aktivitas { get; set; } = [];
}

/// <summary>Statistik dashboard (FR-10). Semua angka dibatasi cakupan data pengguna (BR-11).</summary>
public static class DashboardService
{
    private static readonly string[] UrutanHuruf = ["A", "AB", "B", "BC", "C", "D", "E"];

    public static DataDashboard Ambil()
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        var hariIni = DateOnly.FromDateTime(DateTime.Today);

        var kelasIds = AksesKelas.KelasDiizinkan(db, pengguna);
        var daftarKelas = db.Kelas.AsNoTracking().Include(k => k.Jadwal).Where(k => kelasIds.Contains(k.Id)).ToList();
        var rekapPerKelas = daftarKelas.Select(k => RekapService.Hitung(db, k.Id, hariIni)).ToList();
        var semuaBaris = rekapPerKelas.SelectMany(r => r.Baris).ToList();
        var data = new DataDashboard();

        // Kartu statistik
        data.TotalPraktikan = pengguna.IsKoordinator
            ? db.Praktikan.Count()
            : semuaBaris.Select(b => b.PraktikanId).Distinct().Count();
        data.KelasAktif = daftarKelas.Count(k => !k.IsLocked);

        foreach (var praktikumId in daftarKelas.Select(k => k.PraktikumId).Distinct())
        {
            var modulIds = db.Modul.Where(m => m.PraktikumId == praktikumId).Select(m => m.Id).ToList();
            data.TotalModul += modulIds.Count;
            // Modul berjalan = sudah lewat tanggalnya di minimal satu kelas dalam cakupan pengguna.
            data.ModulBerjalan += modulIds.Count(id => daftarKelas.Any(k =>
                k.PraktikumId == praktikumId && k.Jadwal.Any(j => j.ModulId == id && j.Tanggal <= hariIni)));
        }
        data.RataKehadiran = Perhitungan.Kehadiran(semuaBaris.Sum(b => b.Hadir), semuaBaris.Sum(b => b.Alpa));

        // Grafik batang: rata-rata nilai per modul (hanya dari nilai yang sudah diinput).
        var lebihDariSatuPraktikum = rekapPerKelas.Select(r => r.PraktikumId).Distinct().Count() > 1;
        foreach (var grup in rekapPerKelas.GroupBy(r => r.PraktikumId))
        {
            var contoh = grup.First();
            for (var i = 0; i < contoh.DaftarModul.Count; i++)
            {
                var nilai = grup.SelectMany(r => r.Baris).Select(b => b.NilaiPerModul[i]).OfType<decimal>().ToList();
                if (nilai.Count == 0) continue;
                var label = (lebihDariSatuPraktikum ? contoh.KodePraktikum + " " : "") + "M" + contoh.DaftarModul[i].Nomor;
                data.RataPerModul.Add(new RataModul(label, Perhitungan.Bulatkan(nilai.Average())));
            }
        }

        // Grafik donat: distribusi huruf.
        data.DistribusiHuruf = UrutanHuruf
            .Select(h => (h, semuaBaris.Count(b => b.Huruf == h)))
            .Where(x => x.Item2 > 0)
            .ToList();

        // Tabel praktikan berisiko.
        foreach (var rekap in rekapPerKelas)
            foreach (var baris in rekap.Baris.Where(b => b.Berisiko))
                data.Berisiko.Add(new BarisRisiko(baris.PraktikanId, baris.Nrp, baris.Nama, rekap.Label, baris.AlasanRisiko));

        data.Aktivitas = LogService.Terbaru(8);
        return data;
    }
}
