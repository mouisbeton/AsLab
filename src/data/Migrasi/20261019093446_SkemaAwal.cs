using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AsLab.Data.Migrasi
{
    /// <inheritdoc />
    public partial class SkemaAwal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "praktikan",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nrp = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    nama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    angkatan = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_praktikan", x => x.id);
                    table.CheckConstraint("ck_praktikan_nrp", "LEN(nrp) = 10 AND nrp NOT LIKE '%[^0-9]%'");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    username = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    nama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint("ck_users_role", "role IN ('koordinator','asisten','dosen')");
                });

            migrationBuilder.CreateTable(
                name: "log_aktivitas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    aksi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    detail = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    waktu = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_aktivitas", x => x.id);
                    table.ForeignKey(
                        name: "FK_log_aktivitas_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mata_praktikum",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    kode = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    nama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    semester = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    tahun_ajaran = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    dosen_id = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mata_praktikum", x => x.id);
                    table.ForeignKey(
                        name: "FK_mata_praktikum_users_dosen_id",
                        column: x => x.dosen_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "kelas",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    praktikum_id = table.Column<int>(type: "int", nullable: false),
                    nama_kelas = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    hari = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    jam = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ruang = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    is_locked = table.Column<bool>(type: "bit", nullable: false),
                    dikunci_oleh = table.Column<int>(type: "int", nullable: true),
                    dikunci_pada = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kelas", x => x.id);
                    table.ForeignKey(
                        name: "FK_kelas_mata_praktikum_praktikum_id",
                        column: x => x.praktikum_id,
                        principalTable: "mata_praktikum",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_kelas_users_dikunci_oleh",
                        column: x => x.dikunci_oleh,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "komponen_nilai",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    praktikum_id = table.Column<int>(type: "int", nullable: false),
                    nama = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    bobot = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    urutan = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_komponen_nilai", x => x.id);
                    table.CheckConstraint("ck_komponen_nilai_bobot", "bobot > 0 AND bobot <= 100");
                    table.ForeignKey(
                        name: "FK_komponen_nilai_mata_praktikum_praktikum_id",
                        column: x => x.praktikum_id,
                        principalTable: "mata_praktikum",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "modul",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    praktikum_id = table.Column<int>(type: "int", nullable: false),
                    nomor = table.Column<int>(type: "int", nullable: false),
                    judul = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modul", x => x.id);
                    table.ForeignKey(
                        name: "FK_modul_mata_praktikum_praktikum_id",
                        column: x => x.praktikum_id,
                        principalTable: "mata_praktikum",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "penugasan_asisten",
                columns: table => new
                {
                    kelas_id = table.Column<int>(type: "int", nullable: false),
                    asisten_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_penugasan_asisten", x => new { x.kelas_id, x.asisten_id });
                    table.ForeignKey(
                        name: "FK_penugasan_asisten_kelas_kelas_id",
                        column: x => x.kelas_id,
                        principalTable: "kelas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_penugasan_asisten_users_asisten_id",
                        column: x => x.asisten_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "peserta_kelas",
                columns: table => new
                {
                    kelas_id = table.Column<int>(type: "int", nullable: false),
                    praktikan_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_peserta_kelas", x => new { x.kelas_id, x.praktikan_id });
                    table.ForeignKey(
                        name: "FK_peserta_kelas_kelas_kelas_id",
                        column: x => x.kelas_id,
                        principalTable: "kelas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_peserta_kelas_praktikan_praktikan_id",
                        column: x => x.praktikan_id,
                        principalTable: "praktikan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "absensi",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    modul_id = table.Column<int>(type: "int", nullable: false),
                    kelas_id = table.Column<int>(type: "int", nullable: false),
                    praktikan_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    dicatat_oleh = table.Column<int>(type: "int", nullable: false),
                    waktu = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_absensi", x => x.id);
                    table.CheckConstraint("ck_absensi_status", "status IN ('Hadir','Izin','Sakit','Alpa')");
                    table.ForeignKey(
                        name: "FK_absensi_kelas_kelas_id",
                        column: x => x.kelas_id,
                        principalTable: "kelas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_absensi_modul_modul_id",
                        column: x => x.modul_id,
                        principalTable: "modul",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_absensi_praktikan_praktikan_id",
                        column: x => x.praktikan_id,
                        principalTable: "praktikan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_absensi_users_dicatat_oleh",
                        column: x => x.dicatat_oleh,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "jadwal_modul",
                columns: table => new
                {
                    kelas_id = table.Column<int>(type: "int", nullable: false),
                    modul_id = table.Column<int>(type: "int", nullable: false),
                    tanggal = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jadwal_modul", x => new { x.kelas_id, x.modul_id });
                    table.ForeignKey(
                        name: "FK_jadwal_modul_kelas_kelas_id",
                        column: x => x.kelas_id,
                        principalTable: "kelas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_jadwal_modul_modul_modul_id",
                        column: x => x.modul_id,
                        principalTable: "modul",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nilai",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    modul_id = table.Column<int>(type: "int", nullable: false),
                    kelas_id = table.Column<int>(type: "int", nullable: false),
                    praktikan_id = table.Column<int>(type: "int", nullable: false),
                    nilai_modul = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    dinilai_oleh = table.Column<int>(type: "int", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nilai", x => x.id);
                    table.ForeignKey(
                        name: "FK_nilai_kelas_kelas_id",
                        column: x => x.kelas_id,
                        principalTable: "kelas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_nilai_modul_modul_id",
                        column: x => x.modul_id,
                        principalTable: "modul",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_nilai_praktikan_praktikan_id",
                        column: x => x.praktikan_id,
                        principalTable: "praktikan",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_nilai_users_dinilai_oleh",
                        column: x => x.dinilai_oleh,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "nilai_komponen",
                columns: table => new
                {
                    nilai_id = table.Column<int>(type: "int", nullable: false),
                    komponen_id = table.Column<int>(type: "int", nullable: false),
                    skor = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nilai_komponen", x => new { x.nilai_id, x.komponen_id });
                    table.CheckConstraint("ck_nilai_komponen_skor", "skor BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_nilai_komponen_komponen_nilai_komponen_id",
                        column: x => x.komponen_id,
                        principalTable: "komponen_nilai",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_nilai_komponen_nilai_nilai_id",
                        column: x => x.nilai_id,
                        principalTable: "nilai",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_absensi_dicatat_oleh",
                table: "absensi",
                column: "dicatat_oleh");

            migrationBuilder.CreateIndex(
                name: "IX_absensi_kelas_id",
                table: "absensi",
                column: "kelas_id");

            migrationBuilder.CreateIndex(
                name: "IX_absensi_modul_id_praktikan_id",
                table: "absensi",
                columns: new[] { "modul_id", "praktikan_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_absensi_praktikan_id",
                table: "absensi",
                column: "praktikan_id");

            migrationBuilder.CreateIndex(
                name: "IX_jadwal_modul_modul_id",
                table: "jadwal_modul",
                column: "modul_id");

            migrationBuilder.CreateIndex(
                name: "IX_kelas_dikunci_oleh",
                table: "kelas",
                column: "dikunci_oleh");

            migrationBuilder.CreateIndex(
                name: "IX_kelas_praktikum_id_nama_kelas",
                table: "kelas",
                columns: new[] { "praktikum_id", "nama_kelas" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_komponen_nilai_praktikum_id_nama",
                table: "komponen_nilai",
                columns: new[] { "praktikum_id", "nama" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_log_aktivitas_user_id",
                table: "log_aktivitas",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_log_aktivitas_waktu",
                table: "log_aktivitas",
                column: "waktu");

            migrationBuilder.CreateIndex(
                name: "IX_mata_praktikum_dosen_id",
                table: "mata_praktikum",
                column: "dosen_id");

            migrationBuilder.CreateIndex(
                name: "IX_modul_praktikum_id_nomor",
                table: "modul",
                columns: new[] { "praktikum_id", "nomor" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nilai_dinilai_oleh",
                table: "nilai",
                column: "dinilai_oleh");

            migrationBuilder.CreateIndex(
                name: "IX_nilai_kelas_id",
                table: "nilai",
                column: "kelas_id");

            migrationBuilder.CreateIndex(
                name: "IX_nilai_modul_id_praktikan_id",
                table: "nilai",
                columns: new[] { "modul_id", "praktikan_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nilai_praktikan_id",
                table: "nilai",
                column: "praktikan_id");

            migrationBuilder.CreateIndex(
                name: "IX_nilai_komponen_komponen_id",
                table: "nilai_komponen",
                column: "komponen_id");

            migrationBuilder.CreateIndex(
                name: "IX_penugasan_asisten_asisten_id",
                table: "penugasan_asisten",
                column: "asisten_id");

            migrationBuilder.CreateIndex(
                name: "IX_peserta_kelas_praktikan_id",
                table: "peserta_kelas",
                column: "praktikan_id");

            migrationBuilder.CreateIndex(
                name: "IX_praktikan_nrp",
                table: "praktikan",
                column: "nrp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_username",
                table: "users",
                column: "username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "absensi");

            migrationBuilder.DropTable(
                name: "jadwal_modul");

            migrationBuilder.DropTable(
                name: "log_aktivitas");

            migrationBuilder.DropTable(
                name: "nilai_komponen");

            migrationBuilder.DropTable(
                name: "penugasan_asisten");

            migrationBuilder.DropTable(
                name: "peserta_kelas");

            migrationBuilder.DropTable(
                name: "komponen_nilai");

            migrationBuilder.DropTable(
                name: "nilai");

            migrationBuilder.DropTable(
                name: "kelas");

            migrationBuilder.DropTable(
                name: "modul");

            migrationBuilder.DropTable(
                name: "praktikan");

            migrationBuilder.DropTable(
                name: "mata_praktikum");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
