using AsLab.Data;
using AsLab.Services;

namespace AsLab.Auth;

/// <summary>Data pengguna yang sedang login (kontrak "session pengguna" di PRD 13.4).</summary>
public record Pengguna(int Id, string Nama, string Username, string Role)
{
    public bool IsKoordinator => Role == Peran.Koordinator;
    public bool IsAsisten => Role == Peran.Asisten;
    public bool IsDosen => Role == Peran.Dosen;
}

public static class Sesi
{
    public static Pengguna? Aktif { get; private set; }

    /// <summary>Pengguna aktif; melempar error bila belum login.</summary>
    public static Pengguna Wajib => Aktif ?? throw new AturanException("Sesi berakhir. Silakan login ulang.");

    public static void Masuk(Pengguna pengguna) => Aktif = pengguna;

    public static void Keluar() => Aktif = null;
}
