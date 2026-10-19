namespace AsLab.Data;

public static class Peran
{
    public const string Koordinator = "koordinator";
    public const string Asisten = "asisten";
    public const string Dosen = "dosen";

    public static readonly string[] Semua = [Koordinator, Asisten, Dosen];

    public static string Label(string peran) => peran switch
    {
        Koordinator => "Koordinator",
        Asisten => "Asisten",
        Dosen => "Dosen",
        _ => peran,
    };
}

public static class StatusAbsensi
{
    public const string Hadir = "Hadir";
    public const string Izin = "Izin";
    public const string Sakit = "Sakit";
    public const string Alpa = "Alpa";

    public static readonly string[] Semua = [Hadir, Izin, Sakit, Alpa];
}

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Nama { get; set; } = "";
    public string Role { get; set; } = "";
}

public class MataPraktikum
{
    public int Id { get; set; }
    public string Kode { get; set; } = "";
    public string Nama { get; set; } = "";
    public string Semester { get; set; } = "";
    public string TahunAjaran { get; set; } = "";
    public int? DosenId { get; set; }
    public User? Dosen { get; set; }

    public List<KomponenNilai> DaftarKomponen { get; set; } = [];
    public List<Kelas> DaftarKelas { get; set; } = [];
    public List<Modul> DaftarModul { get; set; } = [];
}

/// <summary>
/// Komponen penilaian sebuah mata praktikum, misalnya Pretest 20%, Praktik 50%, Laporan 30% (BR-01).
/// Total bobot semua komponen satu mata praktikum harus 100.
/// </summary>
public class KomponenNilai
{
    public int Id { get; set; }
    public int PraktikumId { get; set; }
    public MataPraktikum Praktikum { get; set; } = null!;
    public string Nama { get; set; } = "";
    public decimal Bobot { get; set; }
    public int Urutan { get; set; }

    /// <summary>Komponen bawaan untuk mata praktikum baru: Pretest 20, Praktik 50, Laporan 30.</summary>
    public static List<KomponenNilai> Bawaan() =>
    [
        new() { Nama = "Pretest", Bobot = 20, Urutan = 1 },
        new() { Nama = "Praktik", Bobot = 50, Urutan = 2 },
        new() { Nama = "Laporan", Bobot = 30, Urutan = 3 },
    ];
}

public class Kelas
{
    public int Id { get; set; }
    public int PraktikumId { get; set; }
    public MataPraktikum Praktikum { get; set; } = null!;
    public string NamaKelas { get; set; } = "";
    public string Hari { get; set; } = "";
    public string Jam { get; set; } = "";
    public string Ruang { get; set; } = "";

    // Finalisasi per kelas (BR-10): mengunci absensi dan nilai.
    public bool IsLocked { get; set; }
    public int? DikunciOleh { get; set; }
    public DateTime? DikunciPada { get; set; }

    public List<PesertaKelas> Peserta { get; set; } = [];
    public List<PenugasanAsisten> Asisten { get; set; } = [];
    public List<JadwalModul> Jadwal { get; set; } = [];
}

public class Modul
{
    public int Id { get; set; }
    public int PraktikumId { get; set; }
    public MataPraktikum Praktikum { get; set; } = null!;
    public int Nomor { get; set; }
    public string Judul { get; set; } = "";
}

public class JadwalModul
{
    public int KelasId { get; set; }
    public Kelas Kelas { get; set; } = null!;
    public int ModulId { get; set; }
    public Modul Modul { get; set; } = null!;
    public DateOnly Tanggal { get; set; }
}

public class Praktikan
{
    public int Id { get; set; }
    public string Nrp { get; set; } = "";
    public string Nama { get; set; } = "";
    public int Angkatan { get; set; }
    public string Email { get; set; } = "";
    public List<PesertaKelas> Keanggotaan { get; set; } = [];
}

public class PesertaKelas
{
    public int KelasId { get; set; }
    public Kelas Kelas { get; set; } = null!;
    public int PraktikanId { get; set; }
    public Praktikan Praktikan { get; set; } = null!;
}

public class PenugasanAsisten
{
    public int KelasId { get; set; }
    public Kelas Kelas { get; set; } = null!;
    public int AsistenId { get; set; }
    public User Asisten { get; set; } = null!;
}

public class Absensi
{
    public int Id { get; set; }
    public int ModulId { get; set; }
    public Modul Modul { get; set; } = null!;
    public int KelasId { get; set; }
    public Kelas Kelas { get; set; } = null!;
    public int PraktikanId { get; set; }
    public Praktikan Praktikan { get; set; } = null!;
    public string Status { get; set; } = "";
    public int DicatatOleh { get; set; }
    public DateTime Waktu { get; set; }
}

public class Nilai
{
    public int Id { get; set; }
    public int ModulId { get; set; }
    public Modul Modul { get; set; } = null!;
    public int KelasId { get; set; }
    public Kelas Kelas { get; set; } = null!;
    public int PraktikanId { get; set; }
    public Praktikan Praktikan { get; set; } = null!;
    public decimal NilaiModul { get; set; }
    public int DinilaiOleh { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>Skor setiap komponen; nilai modul dihitung dari sini.</summary>
    public List<NilaiKomponen> DaftarSkor { get; set; } = [];
}

/// <summary>Skor seorang praktikan untuk satu komponen pada satu modul, 0–100 (BR-07).</summary>
public class NilaiKomponen
{
    public int NilaiId { get; set; }
    public Nilai Induk { get; set; } = null!;
    public int KomponenId { get; set; }
    public KomponenNilai Komponen { get; set; } = null!;
    public decimal Skor { get; set; }
}

public class LogAktivitas
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Aksi { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime Waktu { get; set; }
}
