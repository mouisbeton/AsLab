using System.Globalization;
using System.IO;
using System.Text;
using AsLab.Auth;
using AsLab.Data;
using CsvHelper;
using CsvHelper.Configuration;

namespace AsLab.Services.Master;

public record HasilImport(int Berhasil, List<string> Gagal);

/// <summary>
/// Import praktikan dari CSV (FR-12, format di PRD 6.1):
/// header nrp,nama,angkatan,email,nama_kelas — kolom nama_kelas boleh kosong.
/// Baris valid diimpor, baris tidak valid dilewati dan dilaporkan.
/// </summary>
public static class ImportCsvService
{
    private static readonly string[] KolomWajib = ["nrp", "nama", "angkatan", "email"];

    public static HasilImport Import(string pathFile, int? praktikumId)
    {
        using var reader = new StreamReader(pathFile, Encoding.UTF8);
        return Import(reader, praktikumId);
    }

    public static HasilImport Import(TextReader reader, int? praktikumId)
    {
        var pengguna = Sesi.Wajib;
        AksesKelas.PastikanKoordinator(pengguna);

        var konfigurasi = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
        };
        using var csv = new CsvReader(reader, konfigurasi);
        if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord == null)
            throw new AturanException("File CSV kosong.");

        var header = csv.HeaderRecord.Select(h => h.Trim().ToLowerInvariant()).ToList();
        var kurang = KolomWajib.Where(k => !header.Contains(k)).ToList();
        if (kurang.Count > 0)
            throw new AturanException($"Kolom wajib tidak ada: {string.Join(", ", kurang)}. Header harus: nrp,nama,angkatan,email,nama_kelas");
        var adaKolomKelas = header.Contains("nama_kelas");

        using var db = Db.Buka();
        var nrpTerdaftar = db.Praktikan.Select(p => p.Nrp).ToHashSet();
        List<Kelas> kelasPraktikum = praktikumId == null ? [] : db.Kelas.Where(k => k.PraktikumId == praktikumId).ToList();

        var gagal = new List<string>();
        var berhasil = 0;
        while (csv.Read())
        {
            var nomorBaris = csv.Parser.Row;
            var nrp = csv.GetField("nrp") ?? "";
            var nama = csv.GetField("nama") ?? "";
            var angkatanTeks = csv.GetField("angkatan") ?? "";
            var email = csv.GetField("email") ?? "";
            var namaKelas = adaKolomKelas ? csv.GetField("nama_kelas") ?? "" : "";

            string? error = PraktikanService.ValidasiNrp(nrp);
            if (error == null && nrpTerdaftar.Contains(nrp)) error = "NRP sudah terdaftar.";
            if (error == null && nama == "") error = "Nama kosong.";
            if (error == null && (!int.TryParse(angkatanTeks, out _) || angkatanTeks.Length != 4)) error = "Angkatan harus tahun 4 digit.";
            if (error == null && email != "" && !PraktikanService.EmailValid(email)) error = "Format email tidak valid.";

            Kelas? kelas = null;
            if (error == null && namaKelas != "")
            {
                var namaBersih = namaKelas.StartsWith("Kelas ", StringComparison.OrdinalIgnoreCase) ? namaKelas[6..].Trim() : namaKelas;
                kelas = kelasPraktikum.FirstOrDefault(k => k.NamaKelas.Equals(namaBersih, StringComparison.OrdinalIgnoreCase));
                if (praktikumId == null) error = "Pilih mata praktikum sebelum import agar kolom nama_kelas bisa dipakai.";
                else if (kelas == null) error = $"Kelas \"{namaKelas}\" tidak ditemukan.";
                else if (kelas.IsLocked) error = $"Kelas {kelas.NamaKelas} sudah dikunci.";
            }

            if (error != null)
            {
                gagal.Add($"Baris {nomorBaris}: {error}");
                continue;
            }

            var praktikan = new Praktikan { Nrp = nrp, Nama = nama, Angkatan = int.Parse(angkatanTeks), Email = email };
            if (kelas != null) praktikan.Keanggotaan.Add(new PesertaKelas { KelasId = kelas.Id });
            db.Praktikan.Add(praktikan);
            nrpTerdaftar.Add(nrp);
            berhasil++;
        }

        LogService.Catat(db, pengguna.Id, "Import CSV", $"{berhasil} praktikan diimpor, {gagal.Count} baris gagal");
        db.SaveChanges();
        return new HasilImport(berhasil, gagal);
    }
}
