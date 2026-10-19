using AsLab.Auth;
using AsLab.Services.Master;
using AsLab.Services.Penilaian;

namespace AsLab.Data;

/// <summary>
/// Data contoh (PRD 13.4): 4 akun, 1 mata praktikum, 2 kelas, 8 modul (5 sudah berlangsung), 30 praktikan.
/// Password akun contoh = username + "123", misalnya koordinator123.
/// </summary>
public static class Seeder
{
    private static readonly string[] JudulModul =
    [
        "Array dan Pointer", "Linked List", "Stack", "Queue",
        "Tree", "Binary Search Tree", "Graph", "Hashing",
    ];

    private static readonly string[] NamaPraktikan =
    [
        "Ahmad Fauzi", "Bella Safitri", "Cahya Nugraha", "Dimas Prasetyo", "Eka Putri Lestari",
        "Fajar Ramadhan", "Gita Anjani", "Hendra Wijaya", "Intan Permata", "Joko Santoso",
        "Kurnia Sari", "Lukman Hakim", "Maya Anggraini", "Nanda Saputra", "Oktavia Rahma",
        "Putra Mahendra", "Qori Aulia", "Rizky Firmansyah", "Salsabila Nur", "Teguh Wibowo",
        "Umi Kalsum", "Vino Aditya", "Wulan Dari", "Yoga Pratama", "Zahra Amelia",
        "Arif Budiman", "Bunga Citra", "Doni Kurniawan", "Elsa Maharani", "Galih Setiawan",
    ];

    public static void Isi(AsLabDbContext db) => Isi(db, DateOnly.FromDateTime(DateTime.Today));

    public static void Isi(AsLabDbContext db, DateOnly hariIni)
    {
        using var transaksi = db.Database.BeginTransaction();

        var koordinator = BuatAkun("koordinator", "Rina Koordinator", Peran.Koordinator);
        var asistenA = BuatAkun("asisten1", "Budi Asisten", Peran.Asisten);
        var asistenB = BuatAkun("asisten2", "Sari Asisten", Peran.Asisten);
        var dosen = BuatAkun("dosen1", "Dr. Andi Pratama", Peran.Dosen);
        db.Users.AddRange(koordinator, asistenA, asistenB, dosen);

        var praktikum = new MataPraktikum
        {
            Kode = "PSD", Nama = "Praktikum Struktur Data", Semester = "Ganjil", TahunAjaran = "2026/2027", Dosen = dosen,
            DaftarKomponen = KomponenNilai.Bawaan(),
        };
        for (var i = 0; i < JudulModul.Length; i++)
            praktikum.DaftarModul.Add(new Modul { Nomor = i + 1, Judul = JudulModul[i] });

        var kelasA = new Kelas { Praktikum = praktikum, NamaKelas = "A", Hari = "Senin", Jam = "07:00–09:30", Ruang = "Lab Pemrograman 1" };
        var kelasB = new Kelas { Praktikum = praktikum, NamaKelas = "B", Hari = "Rabu", Jam = "13:00–15:30", Ruang = "Lab Pemrograman 2" };
        db.MataPraktikum.Add(praktikum);
        db.Kelas.AddRange(kelasA, kelasB);
        db.SaveChanges();

        // Modul ke-5 baru saja berlangsung, modul 6–8 masih akan datang.
        foreach (var modul in praktikum.DaftarModul)
        {
            db.JadwalModul.Add(new JadwalModul { KelasId = kelasA.Id, ModulId = modul.Id, Tanggal = hariIni.AddDays(7 * (modul.Nomor - 5) - 3) });
            db.JadwalModul.Add(new JadwalModul { KelasId = kelasB.Id, ModulId = modul.Id, Tanggal = hariIni.AddDays(7 * (modul.Nomor - 5) - 1) });
        }
        db.PenugasanAsisten.Add(new PenugasanAsisten { KelasId = kelasA.Id, AsistenId = asistenA.Id });
        db.PenugasanAsisten.Add(new PenugasanAsisten { KelasId = kelasB.Id, AsistenId = asistenB.Id });

        // 15 praktikan per kelas; 3 orang terakhir di tiap kelas angkatan 2023.
        var daftarPraktikan = new List<Praktikan>();
        for (var i = 0; i < NamaPraktikan.Length; i++)
        {
            var angkatanLama = i % 15 >= 12;
            var nrp = (angkatanLama ? "50252310" : "50252410") + (i + 1).ToString("00");
            var praktikan = new Praktikan
            {
                Nrp = nrp, Nama = NamaPraktikan[i], Angkatan = angkatanLama ? 2023 : 2024, Email = $"{nrp}@mhs.kampus.ac.id",
            };
            praktikan.Keanggotaan.Add(new PesertaKelas { Kelas = i < 15 ? kelasA : kelasB });
            daftarPraktikan.Add(praktikan);
        }
        db.Praktikan.AddRange(daftarPraktikan);
        db.SaveChanges();

        // Absensi modul 1–5 dan nilainya. Kelas B belum menginput nilai modul 5.
        var komponen = praktikum.DaftarKomponen.OrderBy(k => k.Urutan).ToList(); // Pretest, Praktik, Laporan
        var modulUrut = praktikum.DaftarModul.OrderBy(m => m.Nomor).ToList();
        var acak = new Random(42);
        for (var i = 0; i < daftarPraktikan.Count; i++)
        {
            var praktikan = daftarPraktikan[i];
            var kelas = i < 15 ? kelasA : kelasB;
            var asisten = i < 15 ? asistenA : asistenB;
            var jumlahModulDinilai = i < 15 ? 5 : 4;
            var nilaiDasar = i == 18 ? 42 : acak.Next(62, 94); // praktikan ke-19 sengaja bernilai rendah

            for (var k = 0; k < 5; k++)
            {
                var modul = modulUrut[k];
                var status = StatusAcak(acak);
                if (i == 3 && (k == 1 || k == 3)) status = StatusAbsensi.Alpa; // praktikan ke-4 sengaja sering alpa
                var waktu = DateTime.Today.AddDays(7 * (k - 4) - (i < 15 ? 3 : 1)).AddHours(10);

                db.Absensi.Add(new Absensi
                {
                    ModulId = modul.Id, KelasId = kelas.Id, PraktikanId = praktikan.Id,
                    Status = status, DicatatOleh = asisten.Id, Waktu = waktu,
                });

                if (k < jumlahModulDinilai && status != StatusAbsensi.Alpa)
                {
                    decimal[] skor =
                    [
                        Batasi(nilaiDasar + acak.Next(-12, 9)),
                        Batasi(nilaiDasar + acak.Next(-8, 9)),
                        Batasi(nilaiDasar + acak.Next(-10, 9)),
                    ];
                    var nilai = new Nilai
                    {
                        ModulId = modul.Id, KelasId = kelas.Id, PraktikanId = praktikan.Id,
                        NilaiModul = Perhitungan.NilaiModul(komponen.Select((k, j) => (skor[j], k.Bobot))),
                        DinilaiOleh = asisten.Id, UpdatedAt = waktu.AddDays(2),
                    };
                    for (var j = 0; j < komponen.Count; j++)
                        nilai.DaftarSkor.Add(new NilaiKomponen { KomponenId = komponen[j].Id, Skor = skor[j] });
                    db.Nilai.Add(nilai);
                }
            }
        }

        LogService.Catat(db, koordinator.Id, "Data contoh", "1 mata praktikum, 2 kelas, 8 modul, dan 30 praktikan dibuat");
        LogService.Catat(db, asistenB.Id, "Input absensi", "PSD · Kelas B · M5: 15 status disimpan");
        LogService.Catat(db, asistenA.Id, "Input nilai", "PSD · Kelas A · M5: nilai praktikan disimpan");
        db.SaveChanges();
        transaksi.Commit();
    }

    private static User BuatAkun(string username, string nama, string peran) =>
        new() { Username = username, Nama = nama, Role = peran, PasswordHash = AuthService.Hash(username + "123") };

    private static string StatusAcak(Random acak)
    {
        var angka = acak.Next(100);
        if (angka < 86) return StatusAbsensi.Hadir;
        if (angka < 90) return StatusAbsensi.Izin;
        if (angka < 94) return StatusAbsensi.Sakit;
        return StatusAbsensi.Alpa;
    }

    private static decimal Batasi(int n) => Math.Clamp(n, 0, 100);
}
