using System.Windows;
using System.Windows.Controls;

namespace AsLab.Views.Master;

public partial class AkunView : UserControl
{
    public AkunView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) =>
        {
            if (DataContext is AkunViewModel vm)
                vm.KosongkanPassword = () => KotakPassword.Clear();
        };
    }

    // PasswordBox tidak bisa di-binding (demi keamanan), jadi isinya diteruskan manual ke ViewModel.
    private void KotakPassword_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AkunViewModel vm)
            vm.FormPassword = KotakPassword.Password;
    }
}
