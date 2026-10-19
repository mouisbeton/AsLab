-- Dibuat otomatis dari AsLabDbContext (lihat tests/AsLab.Tests/SkemaTests.cs).
CREATE TABLE [praktikan] (
    [id] int NOT NULL IDENTITY,
    [nrp] nvarchar(200) NOT NULL,
    [nama] nvarchar(200) NOT NULL,
    [angkatan] int NOT NULL,
    [email] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_praktikan] PRIMARY KEY ([id]),
    CONSTRAINT [ck_praktikan_nrp] CHECK (LEN(nrp) = 10 AND nrp NOT LIKE '%[^0-9]%')
);
GO


CREATE TABLE [users] (
    [id] int NOT NULL IDENTITY,
    [username] nvarchar(200) NOT NULL,
    [password_hash] nvarchar(200) NOT NULL,
    [nama] nvarchar(200) NOT NULL,
    [role] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_users] PRIMARY KEY ([id]),
    CONSTRAINT [ck_users_role] CHECK (role IN ('koordinator','asisten','dosen'))
);
GO


CREATE TABLE [log_aktivitas] (
    [id] int NOT NULL IDENTITY,
    [user_id] int NOT NULL,
    [aksi] nvarchar(200) NOT NULL,
    [detail] nvarchar(2000) NOT NULL,
    [waktu] datetime2 NOT NULL,
    CONSTRAINT [PK_log_aktivitas] PRIMARY KEY ([id]),
    CONSTRAINT [FK_log_aktivitas_users_user_id] FOREIGN KEY ([user_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [mata_praktikum] (
    [id] int NOT NULL IDENTITY,
    [kode] nvarchar(200) NOT NULL,
    [nama] nvarchar(200) NOT NULL,
    [semester] nvarchar(200) NOT NULL,
    [tahun_ajaran] nvarchar(200) NOT NULL,
    [dosen_id] int NULL,
    CONSTRAINT [PK_mata_praktikum] PRIMARY KEY ([id]),
    CONSTRAINT [FK_mata_praktikum_users_dosen_id] FOREIGN KEY ([dosen_id]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [kelas] (
    [id] int NOT NULL IDENTITY,
    [praktikum_id] int NOT NULL,
    [nama_kelas] nvarchar(200) NOT NULL,
    [hari] nvarchar(200) NOT NULL,
    [jam] nvarchar(200) NOT NULL,
    [ruang] nvarchar(200) NOT NULL,
    [is_locked] bit NOT NULL,
    [dikunci_oleh] int NULL,
    [dikunci_pada] datetime2 NULL,
    CONSTRAINT [PK_kelas] PRIMARY KEY ([id]),
    CONSTRAINT [FK_kelas_mata_praktikum_praktikum_id] FOREIGN KEY ([praktikum_id]) REFERENCES [mata_praktikum] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_kelas_users_dikunci_oleh] FOREIGN KEY ([dikunci_oleh]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [komponen_nilai] (
    [id] int NOT NULL IDENTITY,
    [praktikum_id] int NOT NULL,
    [nama] nvarchar(200) NOT NULL,
    [bobot] decimal(5,2) NOT NULL,
    [urutan] int NOT NULL,
    CONSTRAINT [PK_komponen_nilai] PRIMARY KEY ([id]),
    CONSTRAINT [ck_komponen_nilai_bobot] CHECK (bobot > 0 AND bobot <= 100),
    CONSTRAINT [FK_komponen_nilai_mata_praktikum_praktikum_id] FOREIGN KEY ([praktikum_id]) REFERENCES [mata_praktikum] ([id]) ON DELETE CASCADE
);
GO


CREATE TABLE [modul] (
    [id] int NOT NULL IDENTITY,
    [praktikum_id] int NOT NULL,
    [nomor] int NOT NULL,
    [judul] nvarchar(200) NOT NULL,
    CONSTRAINT [PK_modul] PRIMARY KEY ([id]),
    CONSTRAINT [FK_modul_mata_praktikum_praktikum_id] FOREIGN KEY ([praktikum_id]) REFERENCES [mata_praktikum] ([id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [penugasan_asisten] (
    [kelas_id] int NOT NULL,
    [asisten_id] int NOT NULL,
    CONSTRAINT [PK_penugasan_asisten] PRIMARY KEY ([kelas_id], [asisten_id]),
    CONSTRAINT [FK_penugasan_asisten_kelas_kelas_id] FOREIGN KEY ([kelas_id]) REFERENCES [kelas] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_penugasan_asisten_users_asisten_id] FOREIGN KEY ([asisten_id]) REFERENCES [users] ([id]) ON DELETE CASCADE
);
GO


CREATE TABLE [peserta_kelas] (
    [kelas_id] int NOT NULL,
    [praktikan_id] int NOT NULL,
    CONSTRAINT [PK_peserta_kelas] PRIMARY KEY ([kelas_id], [praktikan_id]),
    CONSTRAINT [FK_peserta_kelas_kelas_kelas_id] FOREIGN KEY ([kelas_id]) REFERENCES [kelas] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_peserta_kelas_praktikan_praktikan_id] FOREIGN KEY ([praktikan_id]) REFERENCES [praktikan] ([id]) ON DELETE CASCADE
);
GO


CREATE TABLE [absensi] (
    [id] int NOT NULL IDENTITY,
    [modul_id] int NOT NULL,
    [kelas_id] int NOT NULL,
    [praktikan_id] int NOT NULL,
    [status] nvarchar(200) NOT NULL,
    [dicatat_oleh] int NOT NULL,
    [waktu] datetime2 NOT NULL,
    CONSTRAINT [PK_absensi] PRIMARY KEY ([id]),
    CONSTRAINT [ck_absensi_status] CHECK (status IN ('Hadir','Izin','Sakit','Alpa')),
    CONSTRAINT [FK_absensi_kelas_kelas_id] FOREIGN KEY ([kelas_id]) REFERENCES [kelas] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_absensi_modul_modul_id] FOREIGN KEY ([modul_id]) REFERENCES [modul] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_absensi_praktikan_praktikan_id] FOREIGN KEY ([praktikan_id]) REFERENCES [praktikan] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_absensi_users_dicatat_oleh] FOREIGN KEY ([dicatat_oleh]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [jadwal_modul] (
    [kelas_id] int NOT NULL,
    [modul_id] int NOT NULL,
    [tanggal] date NOT NULL,
    CONSTRAINT [PK_jadwal_modul] PRIMARY KEY ([kelas_id], [modul_id]),
    CONSTRAINT [FK_jadwal_modul_kelas_kelas_id] FOREIGN KEY ([kelas_id]) REFERENCES [kelas] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_jadwal_modul_modul_modul_id] FOREIGN KEY ([modul_id]) REFERENCES [modul] ([id]) ON DELETE CASCADE
);
GO


CREATE TABLE [nilai] (
    [id] int NOT NULL IDENTITY,
    [modul_id] int NOT NULL,
    [kelas_id] int NOT NULL,
    [praktikan_id] int NOT NULL,
    [nilai_modul] decimal(5,2) NOT NULL,
    [dinilai_oleh] int NOT NULL,
    [updated_at] datetime2 NOT NULL,
    CONSTRAINT [PK_nilai] PRIMARY KEY ([id]),
    CONSTRAINT [FK_nilai_kelas_kelas_id] FOREIGN KEY ([kelas_id]) REFERENCES [kelas] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_nilai_modul_modul_id] FOREIGN KEY ([modul_id]) REFERENCES [modul] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_nilai_praktikan_praktikan_id] FOREIGN KEY ([praktikan_id]) REFERENCES [praktikan] ([id]) ON DELETE CASCADE,
    CONSTRAINT [FK_nilai_users_dinilai_oleh] FOREIGN KEY ([dinilai_oleh]) REFERENCES [users] ([id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [nilai_komponen] (
    [nilai_id] int NOT NULL,
    [komponen_id] int NOT NULL,
    [skor] decimal(5,2) NOT NULL,
    CONSTRAINT [PK_nilai_komponen] PRIMARY KEY ([nilai_id], [komponen_id]),
    CONSTRAINT [ck_nilai_komponen_skor] CHECK (skor BETWEEN 0 AND 100),
    CONSTRAINT [FK_nilai_komponen_komponen_nilai_komponen_id] FOREIGN KEY ([komponen_id]) REFERENCES [komponen_nilai] ([id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_nilai_komponen_nilai_nilai_id] FOREIGN KEY ([nilai_id]) REFERENCES [nilai] ([id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_absensi_dicatat_oleh] ON [absensi] ([dicatat_oleh]);
GO


CREATE INDEX [IX_absensi_kelas_id] ON [absensi] ([kelas_id]);
GO


CREATE UNIQUE INDEX [IX_absensi_modul_id_praktikan_id] ON [absensi] ([modul_id], [praktikan_id]);
GO


CREATE INDEX [IX_absensi_praktikan_id] ON [absensi] ([praktikan_id]);
GO


CREATE INDEX [IX_jadwal_modul_modul_id] ON [jadwal_modul] ([modul_id]);
GO


CREATE INDEX [IX_kelas_dikunci_oleh] ON [kelas] ([dikunci_oleh]);
GO


CREATE UNIQUE INDEX [IX_kelas_praktikum_id_nama_kelas] ON [kelas] ([praktikum_id], [nama_kelas]);
GO


CREATE UNIQUE INDEX [IX_komponen_nilai_praktikum_id_nama] ON [komponen_nilai] ([praktikum_id], [nama]);
GO


CREATE INDEX [IX_log_aktivitas_user_id] ON [log_aktivitas] ([user_id]);
GO


CREATE INDEX [IX_log_aktivitas_waktu] ON [log_aktivitas] ([waktu]);
GO


CREATE INDEX [IX_mata_praktikum_dosen_id] ON [mata_praktikum] ([dosen_id]);
GO


CREATE UNIQUE INDEX [IX_modul_praktikum_id_nomor] ON [modul] ([praktikum_id], [nomor]);
GO


CREATE INDEX [IX_nilai_dinilai_oleh] ON [nilai] ([dinilai_oleh]);
GO


CREATE INDEX [IX_nilai_kelas_id] ON [nilai] ([kelas_id]);
GO


CREATE UNIQUE INDEX [IX_nilai_modul_id_praktikan_id] ON [nilai] ([modul_id], [praktikan_id]);
GO


CREATE INDEX [IX_nilai_praktikan_id] ON [nilai] ([praktikan_id]);
GO


CREATE INDEX [IX_nilai_komponen_komponen_id] ON [nilai_komponen] ([komponen_id]);
GO


CREATE INDEX [IX_penugasan_asisten_asisten_id] ON [penugasan_asisten] ([asisten_id]);
GO


CREATE INDEX [IX_peserta_kelas_praktikan_id] ON [peserta_kelas] ([praktikan_id]);
GO


CREATE UNIQUE INDEX [IX_praktikan_nrp] ON [praktikan] ([nrp]);
GO


CREATE UNIQUE INDEX [IX_users_username] ON [users] ([username]);
GO
