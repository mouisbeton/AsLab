# AsLab — Sistem Informasi Absensi dan Penilaian Praktikum

Aplikasi desktop (C# WPF, .NET 10) untuk mencatat absensi dan nilai praktikum, menghitung nilai akhir
secara otomatis, membatasi akses per peran, dan memantau praktikan berisiko.
Spesifikasi lengkap ada di [PRD-AsLab.md](PRD-AsLab.md).

## Menjalankan

Prasyarat:
- Windows 10/11 dan [.NET 10 SDK](https://dotnet.microsoft.com/download).
- **SQL Server 2022 Express** (instance `SQLEXPRESS`), misalnya lewat `winget install Microsoft.SQLServer.2022.Express`.
- Opsional: **SSMS** untuk melihat tabel dan data (`winget install Microsoft.SQLServerManagementStudio.22`).

```bash
dotnet run --project src
```

Connection string ada di [src/appsettings.json](src/appsettings.json). Bawaannya memakai
`localhost\SQLEXPRESS` dengan Windows Authentication; ubah file ini bila nama instance-mu berbeda.

Saat pertama kali dijalankan, aplikasi membuat database **AsLab** beserta semua tabelnya (EF Core Migrations),
lalu mengisi data contoh: 1 mata praktikum (PSD), 2 kelas, 8 modul (5 sudah berlangsung), dan 30 praktikan.
Untuk mengulang dari awal, hapus database `AsLab` di SSMS (klik kanan → Delete), lalu jalankan aplikasi lagi.

Ingin membuat tabel secara manual di SSMS seperti di materi kuliah? Jalankan skrip
[src/data/schema.sql](src/data/schema.sql) pada database kosong bernama `AsLab`.

### Akun contoh

| Username | Password | Peran | Catatan |
|---|---|---|---|
| `koordinator` | `koordinator123` | Koordinator | Akses penuh |
| `asisten1` | `asisten1123` | Asisten | Hanya Kelas A |
| `asisten2` | `asisten2123` | Asisten | Hanya Kelas B |
| `dosen1` | `dosen1123` | Dosen | Hanya baca, mata praktikum PSD |

Password ini hanya untuk data contoh. Ganti atau hapus akunnya sebelum aplikasi dipakai sungguhan.

### Menjalankan tes

```bash
dotnet test
```

Tes mencakup skenario PRD Bagian 14 (TC-01 sampai TC-14) dan aturan bisnis BR-01 sampai BR-14.
Tes memakai database terpisah **AsLab_Uji** di SQL Server yang sama, yang dibuat ulang setiap kali tes
dijalankan, jadi database aplikasi tidak tersentuh.

## Cara kerja kode

Alurnya selalu sama: **View (XAML) → ViewModel → Service → Database**.

```
src/
├── appsettings.json Connection string SQL Server
├── data/            Entitas (tabel), AsLabDbContext, Seeder (data contoh), schema.sql (T-SQL untuk SSMS)
│   └── Migrasi/     Migrasi EF Core (dibuat otomatis oleh perintah dotnet ef)
├── auth/            Login, sesi pengguna, AksesKelas (siapa boleh membuka kelas mana)
├── services/
│   ├── Umum.cs      AturanException dan Format angka/tanggal
│   ├── master/      Praktikan, mata praktikum & modul, kelas & peserta & jadwal, akun, import CSV, log
│   ├── penilaian/   Perhitungan (semua rumus BR), absensi, nilai, rekap, kunci kelas
│   ├── dashboard/   Statistik dashboard dan detail praktikan
│   ├── pengaturan/  Komponen nilai (nama & bobot per mata praktikum) dan backup database
│   └── laporan/     Export Excel dan PDF
├── views/
│   ├── master/      Halaman Praktikan, Kelas & Modul, Asisten & Akun
│   ├── akademik/    Halaman Absensi, Input Nilai, Rekap Nilai
│   └── dashboard/   Halaman Dashboard, Detail Praktikan, Pengaturan
└── ui/              Jendela utama + sidebar, tema terang/gelap, style bersama, komponen KartuStatistik
tests/AsLab.Tests/   Tes otomatis (xUnit)
docs/contoh-import.csv   Contoh file untuk demo Import CSV (3 baris valid, 2 baris sengaja salah)
```

Beberapa hal yang perlu diketahui:

- **Aturan bisnis hanya ada di service**, bukan di tampilan. Misalnya rumus nilai ada di
  `services/penilaian/Perhitungan.cs`, dan setiap fungsi diberi komentar nomor BR-nya.
- **Setiap service mengecek hak akses sendiri** lewat `Sesi.Wajib` dan `AksesKelas`. Menyembunyikan menu
  saja tidak cukup.
- **Pesan error untuk pengguna** dilempar sebagai `AturanException("…")`. ViewModel menangkapnya lewat
  `Coba(...)` lalu menampilkannya di bagian atas halaman.
- **ViewModel memakai CommunityToolkit.Mvvm:** `[ObservableProperty] private string nama;` membuat
  properti `Nama` yang otomatis memperbarui tampilan, dan `[RelayCommand] void Simpan()` membuat
  `SimpanCommand` untuk tombol.
- **Log aktivitas** ditambahkan dengan `LogService.Catat(db, userId, aksi, detail)` sebelum `db.SaveChanges()`,
  sehingga data dan log-nya tersimpan bersama.

### Menambah halaman baru

1. Buat `NamaViewModel.cs` (turunan `HalamanViewModel`) dan `NamaView.xaml` di folder `views/...`.
2. Daftarkan pasangan keduanya sebagai `DataTemplate` di `ui/MainWindow.xaml`.
3. Tambahkan item menu (judul, ikon, peran yang boleh) di `ui/MainViewModel.cs`.

### Mengubah struktur database

Ubah entitas di `data/Entitas.cs` atau konfigurasinya di `data/AsLabDbContext.cs`, lalu buat migrasi baru
(ganti `NamaPerubahan` dengan nama yang menjelaskan perubahan, misalnya `TambahKolomTelepon`):

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add NamaPerubahan --project src --output-dir data/Migrasi --namespace AsLab.Data.Migrasi
```

Saat aplikasi dijalankan berikutnya, migrasi diterapkan otomatis ke database (`Database.Migrate()` di `data/Db.cs`),
tanpa menghapus data yang sudah ada. Setelah itu perbarui `data/schema.sql` dengan perintah di bawah.
Tes `SchemaSql_SamaDenganModel` akan gagal selama file ini belum diperbarui.

```bash
ASLAB_TULIS_SKEMA=1 dotnet test
```

## Tips pengembangan (build Debug saja)

- `ASLAB_KONEKSI="Server=localhost\SQLEXPRESS;Database=AsLab_Demo;Trusted_Connection=True;TrustServerCertificate=True;"`
  memakai database lain, misalnya untuk rekaman video demo.
- `ASLAB_DEMO_LOGIN=koordinator` langsung masuk tanpa form login.
  Tambahkan `ASLAB_DEMO_HALAMAN="Rekap Nilai"` untuk langsung membuka halaman tertentu.
