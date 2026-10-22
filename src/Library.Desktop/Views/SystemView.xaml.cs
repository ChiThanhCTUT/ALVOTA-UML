using System.Windows;
using System.Windows.Controls;
using Library.Desktop.ViewModels;

namespace Library.Desktop.Views;

public partial class SystemView : UserControl
{
    public SystemView()
    {
        InitializeComponent();
    }

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SystemViewModel vm && sender is PasswordBox passwordBox)
        {
            vm.NewPassword = passwordBox.Password;
        }
    }
}
