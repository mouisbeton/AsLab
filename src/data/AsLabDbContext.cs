using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Data;

/// <summary>Skema database sesuai PRD Bagian 9 (nama tabel dan kolom snake_case).</summary>
public class AsLabDbContext(DbContextOptions<AsLabDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<MataPraktikum> MataPraktikum => Set<MataPraktikum>();
    public DbSet<KomponenNilai> KomponenNilai => Set<KomponenNilai>();
    public DbSet<NilaiKomponen> NilaiKomponen => Set<NilaiKomponen>();
    public DbSet<Kelas> Kelas => Set<Kelas>();
    public DbSet<Modul> Modul => Set<Modul>();
    public DbSet<JadwalModul> JadwalModul => Set<JadwalModul>();
    public DbSet<Praktikan> Praktikan => Set<Praktikan>();
    public DbSet<PesertaKelas> PesertaKelas => Set<PesertaKelas>();
    public DbSet<PenugasanAsisten> PenugasanAsisten => Set<PenugasanAsisten>();
    public DbSet<Absensi> Absensi => Set<Absensi>();
    public DbSet<Nilai> Nilai => Set<Nilai>();
    public DbSet<LogAktivitas> LogAktivitas => Set<LogAktivitas>();

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        // Semua nilai/skor/bobot 0–100 dengan 2 desimal → decimal(5,2) di SQL Server.
        builder.Properties<decimal>().HavePrecision(5, 2);
        // Teks menjadi nvarchar(200); nvarchar(max) tidak bisa dipakai untuk index/UNIQUE di SQL Server.
        builder.Properties<string>().HaveMaxLength(200);
    }

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<User>(e =>
        {
            e.ToTable("users", t => t.HasCheckConstraint("ck_users_role", "role IN ('koordinator','asisten','dosen')"));
            e.HasIndex(x => x.Username).IsUnique();
        });

        m.Entity<MataPraktikum>(e =>
        {
            e.ToTable("mata_praktikum");
            e.HasOne(x => x.Dosen).WithMany().HasForeignKey(x => x.DosenId).OnDelete(DeleteBehavior.Restrict);
        });

        // Total bobot = 100 melibatkan beberapa baris, jadi dicek di PengaturanService, bukan CHECK.
        m.Entity<KomponenNilai>(e =>
        {
            e.ToTable("komponen_nilai", t => t.HasCheckConstraint("ck_komponen_nilai_bobot", "bobot > 0 AND bobot <= 100"));
            e.HasOne(x => x.Praktikum).WithMany(p => p.DaftarKomponen).HasForeignKey(x => x.PraktikumId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.PraktikumId, x.Nama }).IsUnique();
        });

        m.Entity<NilaiKomponen>(e =>
        {
            e.ToTable("nilai_komponen", t => t.HasCheckConstraint("ck_nilai_komponen_skor", "skor BETWEEN 0 AND 100"));
            e.HasKey(x => new { x.NilaiId, x.KomponenId });
            e.HasOne(x => x.Induk).WithMany(n => n.DaftarSkor).HasForeignKey(x => x.NilaiId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Komponen).WithMany().HasForeignKey(x => x.KomponenId).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Kelas>(e =>
        {
            e.ToTable("kelas");
            e.HasOne(x => x.Praktikum).WithMany(p => p.DaftarKelas).HasForeignKey(x => x.PraktikumId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.DikunciOleh).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PraktikumId, x.NamaKelas }).IsUnique();
        });

        m.Entity<Modul>(e =>
        {
            e.ToTable("modul");
            e.HasOne(x => x.Praktikum).WithMany(p => p.DaftarModul).HasForeignKey(x => x.PraktikumId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PraktikumId, x.Nomor }).IsUnique();
        });

        m.Entity<JadwalModul>(e =>
        {
            e.ToTable("jadwal_modul");
            e.HasKey(x => new { x.KelasId, x.ModulId });
            e.HasOne(x => x.Kelas).WithMany(k => k.Jadwal).HasForeignKey(x => x.KelasId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Modul).WithMany().HasForeignKey(x => x.ModulId).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Praktikan>(e =>
        {
            e.ToTable("praktikan", t => t.HasCheckConstraint("ck_praktikan_nrp", "LEN(nrp) = 10 AND nrp NOT LIKE '%[^0-9]%'"));
            e.HasIndex(x => x.Nrp).IsUnique();
        });

        m.Entity<PesertaKelas>(e =>
        {
            e.ToTable("peserta_kelas");
            e.HasKey(x => new { x.KelasId, x.PraktikanId });
            e.HasOne(x => x.Kelas).WithMany(k => k.Peserta).HasForeignKey(x => x.KelasId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Praktikan).WithMany(p => p.Keanggotaan).HasForeignKey(x => x.PraktikanId).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<PenugasanAsisten>(e =>
        {
            e.ToTable("penugasan_asisten");
            e.HasKey(x => new { x.KelasId, x.AsistenId });
            e.HasOne(x => x.Kelas).WithMany(k => k.Asisten).HasForeignKey(x => x.KelasId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Asisten).WithMany().HasForeignKey(x => x.AsistenId).OnDelete(DeleteBehavior.Cascade);
        });

        m.Entity<Absensi>(e =>
        {
            e.ToTable("absensi", t => t.HasCheckConstraint("ck_absensi_status", "status IN ('Hadir','Izin','Sakit','Alpa')"));
            e.HasIndex(x => new { x.ModulId, x.PraktikanId }).IsUnique();
            e.HasOne(x => x.Modul).WithMany().HasForeignKey(x => x.ModulId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Kelas).WithMany().HasForeignKey(x => x.KelasId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Praktikan).WithMany().HasForeignKey(x => x.PraktikanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.DicatatOleh).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<Nilai>(e =>
        {
            e.ToTable("nilai");
            e.HasIndex(x => new { x.ModulId, x.PraktikanId }).IsUnique();
            e.HasOne(x => x.Modul).WithMany().HasForeignKey(x => x.ModulId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Kelas).WithMany().HasForeignKey(x => x.KelasId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Praktikan).WithMany().HasForeignKey(x => x.PraktikanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.DinilaiOleh).OnDelete(DeleteBehavior.Restrict);
        });

        m.Entity<LogAktivitas>(e =>
        {
            e.ToTable("log_aktivitas");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.Waktu);
            e.Property(x => x.Detail).HasMaxLength(2000); // detail perubahan nilai bisa panjang
        });

        foreach (var entitas in m.Model.GetEntityTypes())
            foreach (var properti in entitas.GetProperties())
                properti.SetColumnName(KeSnakeCase(properti.Name));
    }

    private static string KeSnakeCase(string nama)
    {
        var sb = new StringBuilder(nama.Length + 4);
        for (var i = 0; i < nama.Length; i++)
        {
            var c = nama[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}
