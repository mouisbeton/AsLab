using System.IO;
using System.Windows;
using AsLab.Auth;
using AsLab.Data;
using AsLab.Ui;

namespace AsLab;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Error tak terduga ditampilkan, bukan membuat aplikasi tertutup tiba-tiba.
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show("Terjadi kesalahan tak terduga:\n" + args.Exception.Message, "AsLab",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            // Connection string dari appsettings.json (atau variabel lingkungan ASLAB_KONEKSI).
            Db.GunakanSqlServer(Konfigurasi.ConnectionString());
            Db.Siapkan();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Tidak dapat terhubung ke SQL Server.\n\n" +
                "Pastikan service SQL Server (SQLEXPRESS) berjalan dan connection string di appsettings.json benar.\n\n" +
                "Detail: " + ex.Message,
                "AsLab", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        TemaManager.Terapkan(TemaManager.MuatPreferensi());

#if DEBUG
        // Khusus pengembangan: ASLAB_DEMO_LOGIN=koordinator langsung masuk tanpa form login.
        var demo = Environment.GetEnvironmentVariable("ASLAB_DEMO_LOGIN");
        if (!string.IsNullOrEmpty(demo))
        {
            AuthService.LoginDemo(demo);
            var halaman = Environment.GetEnvironmentVariable("ASLAB_DEMO_HALAMAN") ?? "";
            new MainWindow(halaman).Show();
            if (halaman.StartsWith("detail:") && int.TryParse(halaman[7..], out var praktikanId))
                Navigasi.BukaDetailPraktikan(praktikanId);
            return;
        }
#endif

        new LoginWindow().Show();
    }
}
