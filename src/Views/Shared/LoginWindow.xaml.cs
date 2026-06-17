using CplCassaEventi.ViewModels;
using CplCassaEventi.Views.FrontOffice;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace CplCassaEventi.Views.Shared;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;
    public LoginWindow(LoginViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.LoginSucceeded += OnLoginSucceeded;
        Loaded += (_, _) => UsernameBox.Focus();
    }

    private void PwdBox_PasswordChanged(object s, RoutedEventArgs e)
        => _vm.Password = ((PasswordBox)s).Password;
    private void NewPwdBox_PasswordChanged(object s, RoutedEventArgs e)
        => _vm.NewPassword = ((PasswordBox)s).Password;
    private void ConfirmPwdBox_PasswordChanged(object s, RoutedEventArgs e)
        => _vm.ConfirmPassword = ((PasswordBox)s).Password;

    private void OnLoginSucceeded()
    {
        var main = App.Services.GetRequiredService<MainWindow>();
        if (App.CurrentSettings.KioskMode)
        {
            main.WindowStyle = WindowStyle.None;
            main.WindowState = WindowState.Maximized;
            main.Topmost = true;
        }
        main.Show();
        Close();
    }
}
