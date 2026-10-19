using System.Windows;
using AsLab.Auth;

namespace AsLab.Ui;

public partial class MainWindow : Window
{
    public MainWindow(string? judulHalamanAwal = null)
    {
        InitializeComponent();
        var viewModel = new MainViewModel(judulHalamanAwal);
        viewModel.SaatLogout = () =>
        {
            new LoginWindow().Show();
            Close();
        };
        DataContext = viewModel;
    }
}
