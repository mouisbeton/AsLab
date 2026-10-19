using System.Windows;
using AsLab.Services;
using AsLab.Ui;

namespace AsLab.Auth;

/// <summary>Halaman login (FR-01). Cukup sederhana sehingga memakai code-behind tanpa ViewModel.</summary>
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => KotakUsername.Focus();
    }

    private void Masuk_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            AuthService.Login(KotakUsername.Text, KotakPassword.Password);
        }
        catch (AturanException ex)
        {
            TeksError.Text = ex.Message;
            KotakPassword.Clear();
            KotakPassword.Focus();
            return;
        }

        new MainWindow().Show();
        Close();
    }
}
