using AsLab.Data;
using AsLab.Services;
using Microsoft.EntityFrameworkCore;

namespace AsLab.Auth;

/// <summary>Login dan hash password (FR-01, NFR-01).</summary>
public static class AuthService
{
    /// <summary>Tingkat kesulitan BCrypt. Pengujian menurunkannya agar cepat.</summary>
    public static int WorkFactor { get; set; } = 11;

    public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

    public static bool Cocok(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);

    public static Pengguna Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            throw new AturanException("Isi username dan password.");

        using var db = Db.Buka();
        var user = db.Users.AsNoTracking().FirstOrDefault(u => u.Username == username.Trim());
        if (user == null || !Cocok(password, user.PasswordHash))
            throw new AturanException("Username atau password salah.");

        var pengguna = new Pengguna(user.Id, user.Nama, user.Username, user.Role);
        Sesi.Masuk(pengguna);
        return pengguna;
    }

#if DEBUG
    /// <summary>Hanya build Debug: login tanpa password untuk tangkapan layar otomatis.</summary>
    public static Pengguna LoginDemo(string username)
    {
        using var db = Db.Buka();
        var user = db.Users.AsNoTracking().First(u => u.Username == username);
        var pengguna = new Pengguna(user.Id, user.Nama, user.Username, user.Role);
        Sesi.Masuk(pengguna);
        return pengguna;
    }
#endif
}
