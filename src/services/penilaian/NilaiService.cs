using AsLab.Auth;
using AsLab.Data;
using AsLab.Services.Master;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Penilaian;

/// <summary>Satu baris tabel nilai. Skor[i] adalah skor untuk komponen ke-i di LembarNilai.Komponen.</summary>
public record BarisNilai(int PraktikanId, string Nrp, string Nama, decimal?[] Skor, decimal? NilaiModul);

/// <summary>Skor yang diinput untuk seorang praktikan: id komponen → skor.</summary>
public record InputNilai(int PraktikanId, Dictionary<int, decimal> Skor);

public record LembarNilai(List<InfoKomponen> Komponen, List<BarisNilai> Baris, bool Terkunci);

/// <summary>Input nilai per kelas dan modul (FR-07, BR-01, BR-07).</summary>
public static class NilaiService
{
    /// <summary>Komponen nilai sebuah mata praktikum, urut sesuai urutan tampil.</summary>
    public static List<InfoKomponen> Komponen(AsLabDbContext db, int praktikumId) =>
        db.KomponenNilai.AsNoTracking()
            .Where(k => k.PraktikumId == praktikumId)
            .OrderBy(k => k.Urutan)
            .Select(k => new InfoKomponen(k.Id, k.Nama, k.Bobot))
            .ToList();

    public static LembarNilai Ambil(int kelasId, int modulId)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        AksesKelas.PastikanBoleh(db, pengguna, kelasId);

        var kelas = db.Kelas.AsNoTracking().First(k => k.Id == kelasId);
        var komponen = Komponen(db, kelas.PraktikumId);
        var nilaiTersimpan = db.Nilai.AsNoTracking()
            .Include(n => n.DaftarSkor)
            .Where(n => n.KelasId == kelasId && n.ModulId == modulId)
            .ToList();

        var baris = db.PesertaKelas.AsNoTracking()
            .Where(p => p.KelasId == kelasId)
            .Select(p => p.Praktikan)
            .OrderBy(p => p.Nrp)
            .ToList()
            .Select(p =>
            {
                var nilai = nilaiTersimpan.FirstOrDefault(n => n.PraktikanId == p.Id);
                var skor = komponen
                    .Select(k => nilai?.DaftarSkor.FirstOrDefault(s => s.KomponenId == k.Id)?.Skor)
                    .ToArray();
                return new BarisNilai(p.Id, p.Nrp, p.Nama, skor, nilai?.NilaiModul);
            })
            .ToList();

        return new LembarNilai(komponen, baris, kelas.IsLocked);
    }

    /// <summary>
    /// Menyimpan nilai. Setiap perubahan dicatat di log (T4). Bila ada satu saja nilai tidak valid,
    /// tidak ada yang disimpan (TC-03). Mengembalikan jumlah nilai yang berubah.
    /// </summary>
    public static int Simpan(int kelasId, int modulId, List<InputNilai> data)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        using var transaksi = db.Database.BeginTransaction();

        var kelas = RekapService.KelasBolehDiubah(db, pengguna, kelasId);
        var modul = db.Modul.FirstOrDefault(m => m.Id == modulId && m.PraktikumId == kelas.PraktikumId)
            ?? throw new AturanException("Modul tidak ditemukan pada mata praktikum kelas ini.");
        var komponen = Komponen(db, kelas.PraktikumId);
        var pesertaKelas = db.PesertaKelas.Where(p => p.KelasId == kelasId).Include(p => p.Praktikan).ToList();
        var nilaiLama = db.Nilai.Include(n => n.DaftarSkor).Where(n => n.KelasId == kelasId && n.ModulId == modulId).ToList();
        var labelKelas = Format.NamaKelas(kelas.Praktikum.Kode, kelas.NamaKelas);

        var jumlahBerubah = 0;
        foreach (var input in data)
        {
            var peserta = pesertaKelas.FirstOrDefault(p => p.PraktikanId == input.PraktikanId)
                ?? throw new AturanException("Ada praktikan yang bukan peserta kelas ini.");

            // Komponen yang dikirim harus sama persis dengan komponen mata praktikum saat ini.
            if (input.Skor.Count != komponen.Count || komponen.Any(k => !input.Skor.ContainsKey(k.Id)))
                throw new AturanException("Komponen nilai mata praktikum ini baru saja diubah. Muat ulang halaman lalu isi kembali.");

            // Validasi ulang di service, jangan hanya percaya pada tampilan.
            foreach (var k in komponen)
            {
                var error = Perhitungan.ValidasiKomponen(input.Skor[k.Id]);
                if (error != null)
                    throw new AturanException($"Nilai {k.Nama} {peserta.Praktikan.Nrp} tidak valid: {error}");
            }

            var nilaiModul = Perhitungan.NilaiModul(komponen.Select(k => (input.Skor[k.Id], k.Bobot)));
            var lama = nilaiLama.FirstOrDefault(n => n.PraktikanId == input.PraktikanId);

            if (lama == null)
            {
                var baru = new Nilai
                {
                    ModulId = modulId, KelasId = kelasId, PraktikanId = input.PraktikanId,
                    NilaiModul = nilaiModul, DinilaiOleh = pengguna.Id, UpdatedAt = DateTime.Now,
                };
                foreach (var k in komponen)
                    baru.DaftarSkor.Add(new NilaiKomponen { KomponenId = k.Id, Skor = input.Skor[k.Id] });
                db.Nilai.Add(baru);

                LogService.Catat(db, pengguna.Id, "Input nilai",
                    $"{labelKelas} · M{modul.Nomor} · {peserta.Praktikan.Nrp}: {Teks(komponen, input.Skor)} = {Format.Angka(nilaiModul)}");
                jumlahBerubah++;
                continue;
            }

            var skorLama = lama.DaftarSkor.ToDictionary(s => s.KomponenId, s => s.Skor);
            var adaPerubahan = komponen.Any(k => skorLama.GetValueOrDefault(k.Id, -1) != input.Skor[k.Id]);
            if (!adaPerubahan) continue;

            LogService.Catat(db, pengguna.Id, "Ubah nilai",
                $"{labelKelas} · M{modul.Nomor} · {peserta.Praktikan.Nrp}: " +
                $"{Teks(komponen, skorLama)} ({Format.Angka(lama.NilaiModul)}) → {Teks(komponen, input.Skor)} ({Format.Angka(nilaiModul)})");

            foreach (var k in komponen)
            {
                var skor = lama.DaftarSkor.FirstOrDefault(s => s.KomponenId == k.Id);
                if (skor == null) lama.DaftarSkor.Add(new NilaiKomponen { KomponenId = k.Id, Skor = input.Skor[k.Id] });
                else skor.Skor = input.Skor[k.Id];
            }
            lama.NilaiModul = nilaiModul;
            lama.DinilaiOleh = pengguna.Id;
            lama.UpdatedAt = DateTime.Now;
            jumlahBerubah++;
        }

        db.SaveChanges();
        transaksi.Commit();
        return jumlahBerubah;
    }

    /// <summary>Contoh hasil: "Pretest 80.00, Praktik 90.00, Laporan 70.00".</summary>
    private static string Teks(List<InfoKomponen> komponen, Dictionary<int, decimal> skor) =>
        string.Join(", ", komponen.Select(k => $"{k.Nama} {(skor.TryGetValue(k.Id, out var s) ? Format.Angka(s) : "–")}"));
}
