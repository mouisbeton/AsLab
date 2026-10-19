using AsLab.Auth;
using AsLab.Data;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Penilaian;

/// <summary>Satu baris rekap: nilai dan kehadiran seorang praktikan di satu kelas.</summary>
public class BarisRekap
{
    public int PraktikanId { get; set; }
    public string Nrp { get; set; } = "";
    public string Nama { get; set; } = "";

    /// <summary>Nilai modul ke-1, ke-2, dst. Null bila belum ada nilai.</summary>
    public decimal?[] NilaiPerModul { get; set; } = [];

    public decimal NilaiAkhir { get; set; }
    public decimal? NilaiSementara { get; set; }
    public decimal? Kehadiran { get; set; }
    public int Hadir { get; set; }
    public int Izin { get; set; }
    public int Sakit { get; set; }
    public int Alpa { get; set; }

    /// <summary>Huruf dari nilai akhir bila praktikum selesai, selain itu dari nilai sementara.</summary>
    public string Huruf { get; set; } = "–";
    public string Status { get; set; } = "";
    public bool Berisiko { get; set; }

    public string KehadiranTeks => Format.Persen(Kehadiran);

    public string AlasanRisiko
    {
        get
        {
            var alasan = new List<string>();
            if (Kehadiran < Perhitungan.BatasKehadiran) alasan.Add("Hadir " + Format.Persen(Kehadiran));
            if (NilaiSementara < Perhitungan.BatasLulus) alasan.Add("Nilai " + Format.Angka(NilaiSementara));
            return string.Join(", ", alasan);
        }
    }
}

/// <summary>Rekap satu kelas (dipakai halaman Rekap, Dashboard, Detail, dan Export).</summary>
public class RekapKelas
{
    public int KelasId { get; set; }
    public string NamaKelas { get; set; } = "";
    public int PraktikumId { get; set; }
    public string KodePraktikum { get; set; } = "";
    public string NamaPraktikum { get; set; } = "";
    public string Semester { get; set; } = "";
    public string TahunAjaran { get; set; } = "";
    public string NamaDosen { get; set; } = "–";
    public List<string> NamaAsisten { get; set; } = [];
    public List<Modul> DaftarModul { get; set; } = [];
    public bool Terkunci { get; set; }
    public bool Selesai { get; set; }
    public List<BarisRekap> Baris { get; set; } = [];

    public string Label => Format.NamaKelas(KodePraktikum, NamaKelas);
    public List<string> LabelModul => DaftarModul.Select(m => "M" + m.Nomor).ToList();
}

public static class RekapService
{
    /// <summary>Rekap kelas untuk pengguna yang sedang login (dengan cek akses BR-11).</summary>
    public static RekapKelas Hitung(int kelasId)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        AksesKelas.PastikanBoleh(db, pengguna, kelasId);
        return Hitung(db, kelasId, DateOnly.FromDateTime(DateTime.Today));
    }

    /// <summary>Menghitung rekap tanpa cek akses. Pemanggil wajib sudah mengecek akses.</summary>
    public static RekapKelas Hitung(AsLabDbContext db, int kelasId, DateOnly hariIni)
    {
        var kelas = db.Kelas.AsNoTracking()
            .Include(k => k.Praktikum).ThenInclude(p => p.Dosen)
            .Include(k => k.Asisten).ThenInclude(a => a.Asisten)
            .Include(k => k.Jadwal)
            .Include(k => k.Peserta).ThenInclude(p => p.Praktikan)
            .FirstOrDefault(k => k.Id == kelasId)
            ?? throw new AturanException("Kelas tidak ditemukan.");

        var daftarModul = db.Modul.AsNoTracking().Where(m => m.PraktikumId == kelas.PraktikumId).OrderBy(m => m.Nomor).ToList();
        var semuaNilai = db.Nilai.AsNoTracking().Where(n => n.KelasId == kelasId).ToList();
        var semuaAbsensi = db.Absensi.AsNoTracking().Where(a => a.KelasId == kelasId).ToList();

        // Modul yang sudah dinilai di kelas ini = modul yang punya minimal satu nilai (BR-02).
        var modulDinilai = semuaNilai.Select(n => n.ModulId).Distinct().ToList();

        // BR-04: praktikum selesai bila kelas sudah dikunci, atau semua modul sudah lewat dan sudah dinilai.
        var semuaModulLewat = daftarModul.Count > 0 && daftarModul.All(m =>
            kelas.Jadwal.Any(j => j.ModulId == m.Id && j.Tanggal <= hariIni));
        var selesai = kelas.IsLocked || (semuaModulLewat && daftarModul.All(m => modulDinilai.Contains(m.Id)));

        var rekap = new RekapKelas
        {
            KelasId = kelas.Id,
            NamaKelas = kelas.NamaKelas,
            PraktikumId = kelas.PraktikumId,
            KodePraktikum = kelas.Praktikum.Kode,
            NamaPraktikum = kelas.Praktikum.Nama,
            Semester = kelas.Praktikum.Semester,
            TahunAjaran = kelas.Praktikum.TahunAjaran,
            NamaDosen = kelas.Praktikum.Dosen?.Nama ?? "–",
            NamaAsisten = kelas.Asisten.Select(a => a.Asisten.Nama).OrderBy(n => n).ToList(),
            DaftarModul = daftarModul,
            Terkunci = kelas.IsLocked,
            Selesai = selesai,
        };

        foreach (var praktikan in kelas.Peserta.Select(p => p.Praktikan).OrderBy(p => p.Nrp))
        {
            var nilaiPraktikan = semuaNilai.Where(n => n.PraktikanId == praktikan.Id).ToList();
            var absensiPraktikan = semuaAbsensi.Where(a => a.PraktikanId == praktikan.Id).ToList();

            var baris = new BarisRekap
            {
                PraktikanId = praktikan.Id,
                Nrp = praktikan.Nrp,
                Nama = praktikan.Nama,
                NilaiPerModul = daftarModul
                    .Select(m => nilaiPraktikan.FirstOrDefault(n => n.ModulId == m.Id)?.NilaiModul)
                    .ToArray(),
                Hadir = absensiPraktikan.Count(a => a.Status == StatusAbsensi.Hadir),
                Izin = absensiPraktikan.Count(a => a.Status == StatusAbsensi.Izin),
                Sakit = absensiPraktikan.Count(a => a.Status == StatusAbsensi.Sakit),
                Alpa = absensiPraktikan.Count(a => a.Status == StatusAbsensi.Alpa),
            };

            var nilaiModul = nilaiPraktikan.Select(n => n.NilaiModul).ToList();
            baris.NilaiAkhir = Perhitungan.NilaiAkhir(nilaiModul, daftarModul.Count);
            baris.NilaiSementara = Perhitungan.NilaiSementara(nilaiModul, modulDinilai.Count);
            baris.Kehadiran = Perhitungan.Kehadiran(baris.Hadir, baris.Alpa);
            baris.Status = Perhitungan.Status(baris.Kehadiran, baris.NilaiAkhir, selesai);
            baris.Berisiko = Perhitungan.Berisiko(baris.Kehadiran, baris.NilaiSementara);

            var nilaiUntukHuruf = selesai ? baris.NilaiAkhir : baris.NilaiSementara;
            baris.Huruf = nilaiUntukHuruf.HasValue ? Perhitungan.Huruf(nilaiUntukHuruf.Value) : "–";

            rekap.Baris.Add(baris);
        }

        return rekap;
    }

    /// <summary>Mengambil kelas yang boleh diubah: harus punya akses dan belum dikunci (BR-09, BR-10).</summary>
    public static Kelas KelasBolehDiubah(AsLabDbContext db, Pengguna pengguna, int kelasId)
    {
        AksesKelas.PastikanBolehMengubah(pengguna);
        AksesKelas.PastikanBoleh(db, pengguna, kelasId);
        var kelas = db.Kelas.Include(k => k.Praktikum).First(k => k.Id == kelasId);
        if (kelas.IsLocked)
            throw new AturanException("Kelas ini sudah difinalisasi dan dikunci. Minta koordinator membuka kunci untuk mengubah data.");
        return kelas;
    }
}
