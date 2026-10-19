using AsLab.Auth;
using AsLab.Data;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Services.Master;

/// <summary>Pilihan kelas untuk dropdown.</summary>
public record OpsiKelas(int Id, int PraktikumId, string Label, bool Terkunci)
{
    public override string ToString() => Label;
}

/// <summary>Pilihan modul untuk dropdown, beserta tanggalnya di kelas terpilih.</summary>
public record OpsiModul(int Id, int Nomor, string Judul, DateOnly? Tanggal)
{
    public string Label => $"M{Nomor} · {Judul}" + (Tanggal.HasValue ? $"  ({Format.Tanggal(Tanggal.Value)})" : "");

    public override string ToString() => Label;
}

public record BarisKelas(int Id, string NamaKelas, string Hari, string Jam, string Ruang, int JumlahPeserta, string Asisten, bool Terkunci)
{
    public string StatusKunci => Terkunci ? "Dikunci" : "Terbuka";
}

public record InputKelas(int? Id, int PraktikumId, string NamaKelas, string Hari, string Jam, string Ruang);

public record BarisPeserta(int PraktikanId, string Nrp, string Nama)
{
    public string Label => $"{Nrp} – {Nama}";

    public override string ToString() => Label;
}

public record BarisJadwal(int ModulId, int Nomor, string Judul, DateOnly? Tanggal);

/// <summary>Kelas, peserta kelas, dan jadwal modul per kelas (FR-04).</summary>
public static class KelasService
{
    public static readonly string[] DaftarHari = ["Senin", "Selasa", "Rabu", "Kamis", "Jumat", "Sabtu"];

    /// <summary>Kelas yang boleh diakses pengguna aktif (untuk dropdown di Absensi, Nilai, Rekap).</summary>
    public static List<OpsiKelas> UntukPengguna()
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        var ids = AksesKelas.KelasDiizinkan(db, pengguna);
        return db.Kelas.AsNoTracking().Include(k => k.Praktikum)
            .Where(k => ids.Contains(k.Id))
            .OrderBy(k => k.Praktikum.Kode).ThenBy(k => k.NamaKelas)
            .ToList()
            .Select(k => new OpsiKelas(k.Id, k.PraktikumId, Format.NamaKelas(k.Praktikum.Kode, k.NamaKelas), k.IsLocked))
            .ToList();
    }

    /// <summary>Modul mata praktikum suatu kelas beserta tanggalnya di kelas tersebut.</summary>
    public static List<OpsiModul> ModulKelas(int kelasId)
    {
        var pengguna = Sesi.Wajib;
        using var db = Db.Buka();
        AksesKelas.PastikanBoleh(db, pengguna, kelasId);

        var kelas = db.Kelas.AsNoTracking().Include(k => k.Jadwal).First(k => k.Id == kelasId);
        return db.Modul.AsNoTracking().Where(m => m.PraktikumId == kelas.PraktikumId).OrderBy(m => m.Nomor).ToList()
            .Select(m => new OpsiModul(m.Id, m.Nomor, m.Judul, kelas.Jadwal.FirstOrDefault(j => j.ModulId == m.Id)?.Tanggal))
            .ToList();
    }

    public static List<BarisKelas> DaftarKelas(int praktikumId)
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        return db.Kelas.AsNoTracking()
            .Include(k => k.Peserta)
            .Include(k => k.Asisten).ThenInclude(a => a.Asisten)
            .Where(k => k.PraktikumId == praktikumId)
            .OrderBy(k => k.NamaKelas)
            .ToList()
            .Select(k => new BarisKelas(k.Id, k.NamaKelas, k.Hari, k.Jam, k.Ruang, k.Peserta.Count,
                k.Asisten.Count == 0 ? "–" : string.Join(", ", k.Asisten.Select(a => a.Asisten.Nama)), k.IsLocked))
            .ToList();
    }

    public static int Simpan(InputKelas input)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        var nama = input.NamaKelas.Trim();
        if (nama == "") throw new AturanException("Nama kelas wajib diisi, contoh A.");
        if (!DaftarHari.Contains(input.Hari)) throw new AturanException("Pilih hari pelaksanaan kelas.");

        using var db = Db.Buka();
        if (db.Kelas.Any(k => k.PraktikumId == input.PraktikumId && k.NamaKelas == nama && k.Id != input.Id))
            throw new AturanException($"Kelas {nama} sudah ada di mata praktikum ini.");

        Kelas kelas;
        if (input.Id == null)
        {
            kelas = new Kelas { PraktikumId = input.PraktikumId };
            db.Kelas.Add(kelas);
        }
        else
        {
            kelas = db.Kelas.Find(input.Id.Value) ?? throw new AturanException("Kelas tidak ditemukan.");
        }

        kelas.NamaKelas = nama;
        kelas.Hari = input.Hari;
        kelas.Jam = input.Jam.Trim();
        kelas.Ruang = input.Ruang.Trim();
        LogService.Catat(db, pengguna.Id, input.Id == null ? "Tambah kelas" : "Ubah kelas", $"Kelas {nama}");
        db.SaveChanges();
        return kelas.Id;
    }

    public static void Hapus(int id)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var kelas = db.Kelas.Find(id) ?? throw new AturanException("Kelas tidak ditemukan.");
        if (kelas.IsLocked) throw new AturanException("Kelas yang sudah dikunci tidak dapat dihapus.");
        if (db.Absensi.Any(a => a.KelasId == id) || db.Nilai.Any(n => n.KelasId == id))
            throw new AturanException("Kelas sudah memiliki data absensi atau nilai sehingga tidak dapat dihapus.");

        db.Kelas.Remove(kelas);
        LogService.Catat(db, pengguna.Id, "Hapus kelas", $"Kelas {kelas.NamaKelas}");
        db.SaveChanges();
    }

    public static List<BarisPeserta> Peserta(int kelasId)
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        return db.PesertaKelas.AsNoTracking().Where(p => p.KelasId == kelasId)
            .Select(p => p.Praktikan).OrderBy(p => p.Nrp)
            .Select(p => new BarisPeserta(p.Id, p.Nrp, p.Nama)).ToList();
    }

    /// <summary>Praktikan yang belum terdaftar di kelas mana pun pada mata praktikum yang sama (BR-14).</summary>
    public static List<BarisPeserta> KandidatPeserta(int kelasId)
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        var praktikumId = db.Kelas.Where(k => k.Id == kelasId).Select(k => k.PraktikumId).First();
        var sudahTerdaftar = db.PesertaKelas.Where(p => p.Kelas.PraktikumId == praktikumId).Select(p => p.PraktikanId).ToList();
        return db.Praktikan.AsNoTracking().Where(p => !sudahTerdaftar.Contains(p.Id)).OrderBy(p => p.Nrp)
            .Select(p => new BarisPeserta(p.Id, p.Nrp, p.Nama)).ToList();
    }

    public static void TambahPeserta(int kelasId, int praktikanId)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var kelas = db.Kelas.Find(kelasId) ?? throw new AturanException("Kelas tidak ditemukan.");
        if (kelas.IsLocked) throw new AturanException("Kelas sudah dikunci. Buka kunci terlebih dahulu.");

        var kelasLain = db.PesertaKelas
            .Where(p => p.PraktikanId == praktikanId && p.Kelas.PraktikumId == kelas.PraktikumId)
            .Select(p => p.Kelas.NamaKelas)
            .FirstOrDefault();
        if (kelasLain != null)
            throw new AturanException($"Praktikan sudah terdaftar di Kelas {kelasLain} pada mata praktikum ini.");

        var praktikan = db.Praktikan.Find(praktikanId) ?? throw new AturanException("Praktikan tidak ditemukan.");
        db.PesertaKelas.Add(new PesertaKelas { KelasId = kelasId, PraktikanId = praktikanId });
        LogService.Catat(db, pengguna.Id, "Tambah peserta", $"{praktikan.Nrp} ke Kelas {kelas.NamaKelas}");
        db.SaveChanges();
    }

    public static void KeluarkanPeserta(int kelasId, int praktikanId)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var kelas = db.Kelas.Find(kelasId) ?? throw new AturanException("Kelas tidak ditemukan.");
        if (kelas.IsLocked) throw new AturanException("Kelas sudah dikunci. Buka kunci terlebih dahulu.");
        if (db.Absensi.Any(a => a.KelasId == kelasId && a.PraktikanId == praktikanId) ||
            db.Nilai.Any(n => n.KelasId == kelasId && n.PraktikanId == praktikanId))
            throw new AturanException("Praktikan sudah memiliki absensi atau nilai di kelas ini sehingga tidak dapat dikeluarkan.");

        var peserta = db.PesertaKelas.Include(p => p.Praktikan)
            .FirstOrDefault(p => p.KelasId == kelasId && p.PraktikanId == praktikanId)
            ?? throw new AturanException("Praktikan bukan peserta kelas ini.");
        db.PesertaKelas.Remove(peserta);
        LogService.Catat(db, pengguna.Id, "Keluarkan peserta", $"{peserta.Praktikan.Nrp} dari Kelas {kelas.NamaKelas}");
        db.SaveChanges();
    }

    public static List<BarisJadwal> Jadwal(int kelasId)
    {
        AksesKelas.PastikanKoordinator(Sesi.Wajib);
        using var db = Db.Buka();
        var kelas = db.Kelas.AsNoTracking().Include(k => k.Jadwal).First(k => k.Id == kelasId);
        return db.Modul.AsNoTracking().Where(m => m.PraktikumId == kelas.PraktikumId).OrderBy(m => m.Nomor).ToList()
            .Select(m => new BarisJadwal(m.Id, m.Nomor, m.Judul, kelas.Jadwal.FirstOrDefault(j => j.ModulId == m.Id)?.Tanggal))
            .ToList();
    }

    /// <summary>Menyimpan tanggal setiap modul di kelas ini. Tanggal kosong menghapus jadwal modul itu.</summary>
    public static void SimpanJadwal(int kelasId, List<BarisJadwal> jadwal)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        using var db = Db.Buka();
        var kelas = db.Kelas.Find(kelasId) ?? throw new AturanException("Kelas tidak ditemukan.");
        var lama = db.JadwalModul.Where(j => j.KelasId == kelasId).ToList();

        foreach (var baris in jadwal)
        {
            var ada = lama.FirstOrDefault(j => j.ModulId == baris.ModulId);
            if (baris.Tanggal == null)
            {
                if (ada != null) db.JadwalModul.Remove(ada);
            }
            else if (ada == null)
            {
                db.JadwalModul.Add(new JadwalModul { KelasId = kelasId, ModulId = baris.ModulId, Tanggal = baris.Tanggal.Value });
            }
            else
            {
                ada.Tanggal = baris.Tanggal.Value;
            }
        }

        LogService.Catat(db, pengguna.Id, "Ubah jadwal modul", $"Kelas {kelas.NamaKelas}");
        db.SaveChanges();
    }
}
