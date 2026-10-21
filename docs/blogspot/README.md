# Panduan Posting Laporan AsLab ke Blogspot

Dokumen ini untuk **agen yang memposting laporan** proyek AsLab (mata kuliah PBKK) ke Blogger/Blogspot.
Ikuti langkahnya berurutan. Semua path di bawah relatif terhadap folder repo `AsLab/`.

## Bahan yang tersedia

| File | Isi |
|---|---|
| `docs/blogspot/narasi.html` | Isi artikel lengkap dalam HTML, siap ditempel di **HTML view** Blogger |
| `docs/screenshots/*.png` | 17 screenshot aplikasi (1920×1032, kecuali login 440×560) |
| `docs/contoh-export/rekap-PSD-kelas-A-halaman1.png` | Gambar hasil export PDF |
| `docs/diagram/arsitektur.png`, `docs/diagram/erd.png` | Diagram arsitektur dan ERD |

Di dalam `narasi.html` ada dua jenis penanda:
- `[[GAMBAR NN | path/file.png]]` harus diganti dengan gambar yang diunggah. Total ada **20 penanda**.
- `{{...}}` adalah data yang harus diisi tim, seperti nama, NRP, video, dan tautan.

## Aturan untuk agen

1. **Jangan mengarang data.** Bila isi `{{...}}` (nama anggota, NRP, tautan video, tautan GitHub/PRD) belum diberikan
   oleh pengguna, tanyakan dulu kepada pengguna. Jangan menebak dan jangan membiarkan `{{` tersisa di postingan.
2. **Login dilakukan pengguna sendiri.** Jangan mengetik password. Bila Blogger meminta login, minta pengguna login.
3. **Jangan menekan "Publikasikan / Publish" sebelum pengguna menyetujui** hasil pratinjau secara eksplisit.
   Sebelum itu, simpan saja sebagai draf.
4. Repo GitHub `mouisbeton/AsLab` saat ini **privat**, jadi gambar tidak bisa ditautkan dari GitHub dan harus
   diunggah ke Blogger. Untuk baris "Source code", tanyakan ke pengguna: repo dijadikan publik (lalu pakai
   `https://github.com/mouisbeton/AsLab`), atau barisnya dihapus.

## Langkah posting

### 1. Buat postingan baru
1. Buka https://www.blogger.com dan pastikan blog yang benar terpilih di kiri atas.
2. Klik **+ Postingan baru** (*New Post*).
3. Isi judul:
   `AsLab — Sistem Informasi Absensi dan Penilaian Praktikum (WPF + SQL Server) | PBKK`

### 2. Tempel isi artikel
1. Klik ikon pensil di kiri atas toolbar editor, lalu pilih **Tampilan HTML** (*HTML view*).
2. Buka `docs/blogspot/narasi.html`. Salin semua isinya **mulai dari baris `<p>Pada mata kuliah`**
   (lewati blok komentar `<!-- ... -->` di paling atas), lalu tempel ke editor.
3. Kembali ke **Tampilan menulis** (*Compose view*) lewat ikon pensil yang sama.

### 3. Ganti penanda gambar (lakukan 20 kali, urut dari atas)
Untuk setiap baris `[[GAMBAR NN | path]]`:
1. Blok seluruh teks penanda (termasuk `[[` dan `]]`), lalu hapus. Biarkan kursor di baris kosong itu.
2. Klik ikon **Sisipkan gambar** (*Insert image*) → **Unggah dari komputer** (*Upload from computer*), lalu pilih file sesuai path di penanda.
3. Klik gambar yang sudah masuk, lalu atur ukurannya ke **Sangat besar** (*X-Large*) dan posisinya ke **Tengah**.
4. Klik ikon pensil/opsi pada gambar dan isi **Teks alternatif** (*Alt text*) sesuai tabel di bawah.
5. Jangan hapus baris keterangan miring (*italic*) di bawah gambar.

| Penanda | File | Alt text |
|---|---|---|
| GAMBAR 03 | `docs/screenshots/03-dashboard.png` | Dashboard AsLab dengan kartu statistik dan grafik |
| GAMBAR 01 | `docs/screenshots/01-login.png` | Halaman login AsLab |
| GAMBAR 02 | `docs/screenshots/02-login-gagal.png` | Pesan error saat password salah |
| GAMBAR 04 | `docs/screenshots/04-dashboard-gelap.png` | Dashboard AsLab dengan tema gelap |
| GAMBAR 05 | `docs/screenshots/05-praktikan.png` | Tabel data praktikan dengan pencarian dan filter |
| GAMBAR 06 | `docs/screenshots/06-praktikan-validasi.png` | Validasi NRP pada form tambah praktikan |
| GAMBAR 07 | `docs/screenshots/07-kelas-modul.png` | Halaman kelas dan modul dengan peserta dan jadwal |
| GAMBAR 08 | `docs/screenshots/08-asisten-akun.png` | Pengelolaan akun dan penugasan asisten ke kelas |
| GAMBAR 09 | `docs/screenshots/09-absensi.png` | Pencatatan absensi praktikan |
| GAMBAR 10 | `docs/screenshots/10-input-nilai.png` | Input nilai dengan nilai modul otomatis |
| GAMBAR 11 | `docs/screenshots/11-input-nilai-validasi.png` | Sel nilai tidak valid ditandai merah |
| GAMBAR 15 | `docs/screenshots/15-pengaturan.png` | Pengaturan komponen nilai per mata praktikum |
| GAMBAR 12 | `docs/screenshots/12-rekap-nilai.png` | Rekap nilai kelas saat semester berjalan |
| GAMBAR 13 | `docs/screenshots/13-rekap-terkunci.png` | Rekap nilai setelah difinalisasi dan dikunci |
| GAMBAR 18 | `docs/contoh-export/rekap-PSD-kelas-A-halaman1.png` | Hasil export rekap nilai ke PDF |
| GAMBAR 14 | `docs/screenshots/14-detail-praktikan.png` | Detail praktikan dengan grafik progres nilai |
| GAMBAR 16 | `docs/screenshots/16-dashboard-asisten.png` | Dashboard untuk asisten yang hanya melihat kelasnya |
| GAMBAR 17 | `docs/screenshots/17-rekap-dosen.png` | Rekap nilai untuk dosen (hanya baca) |
| GAMBAR 19 | `docs/diagram/arsitektur.png` | Diagram arsitektur aplikasi AsLab |
| GAMBAR 20 | `docs/diagram/erd.png` | Entity Relationship Diagram database AsLab |

Urutan di tabel sama dengan urutan kemunculan penanda di artikel. Nomor penanda mengikuti nomor file, bukan urutan tampil.

### 4. Isi data `{{...}}`
1. **Anggota kelompok:** ganti `{{NAMA ANGGOTA n}}` dan `{{NRP n}}` dengan data dari pengguna.
2. **Video demo:** hapus teks `{{TEMPEL VIDEO ...}}`, lalu klik ikon **Sisipkan video** (*Insert video*) → **YouTube** dan
   tempel tautan video dari pengguna. Bila belum ada video, tanyakan ke pengguna apakah bagian ini dihapus dulu.
3. **Tautan:** isi tautan GitHub (lihat aturan nomor 4) dan tautan PRD. Bila repo publik, tautan PRD adalah
   `https://github.com/mouisbeton/AsLab/blob/main/PRD-AsLab.md`. Bila tidak, tanyakan ke pengguna (misalnya tautan Google Drive).
4. Cari teks `{{` dan `[[` di editor (Ctrl+F). **Keduanya harus sudah tidak ada.**

### 5. Pengaturan postingan (panel kanan)
- **Label:** `PBKK`, `WPF`, `SQL Server`, `C#`, `Rich UI`, `Tugas Kelompok`
- **Tautan permanen (Permalink):** pilih *Tautan permanen khusus* → `aslab-sistem-informasi-absensi-penilaian-praktikum`
- **Deskripsi penelusuran (Search description):**
  `AsLab: aplikasi desktop WPF dan SQL Server untuk absensi dan penilaian praktikum, dengan dashboard, akses per peran, export Excel/PDF. Tugas PBKK.`
- **Opsi:** komentar pembaca diizinkan (bawaan).

### 6. Periksa sebelum publikasi
1. Klik **Pratinjau** (*Preview*) dan pastikan:
   - ke-20 gambar tampil dan tidak ada gambar ganda,
   - tidak ada teks `[[GAMBAR` maupun `{{` yang tersisa,
   - tiga tabel (anggota, hak akses, teknologi) tampil rapi,
   - video YouTube dapat diputar (bila ada).
2. Tunjukkan hasil pratinjau ke pengguna. **Publikasikan hanya setelah pengguna menyetujui.**
3. Setelah dipublikasikan, berikan URL postingan ke pengguna.

## Bila ada masalah

| Masalah | Solusi |
|---|---|
| Tabel kehilangan garis di Compose view | Normal. Garis tetap tampil di pratinjau karena atribut `border="1"` |
| Gambar terlalu kecil | Klik gambar, lalu pilih ukuran *X-Large* atau *Original size* |
| Format berantakan setelah pindah tampilan | Ulangi langkah 2 di postingan baru, lalu kerjakan langkah 3 dari awal |
| Penanda gambar terpecah dua baris | Hapus kedua bagian penanda, lalu sisipkan gambarnya sekali saja |

## Memperbarui gambar (untuk tim, bukan agen posting)

Screenshot diambil dari aplikasi yang berjalan dengan database demo `AsLab_Demo` (data contoh bawaan).
Bila tampilan aplikasi berubah, jalankan aplikasi, ambil ulang gambar dengan nama file yang sama di
`docs/screenshots/`, lalu ganti gambar yang bersangkutan di postingan.
Contoh export dibuat ulang dengan perintah:

```bash
ASLAB_TULIS_CONTOH=1 dotnet test --filter ContohExport
```
