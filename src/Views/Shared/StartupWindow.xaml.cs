using CassaEventiAI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace CassaEventiAI.Views.Shared;

public partial class StartupWindow : Window
{
    private readonly StartupViewModel _vm;
    public StartupWindow(StartupViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.ProceedToLogin += OnProceedToLogin;
    }

    private void AdminPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.AdminPassword = ((PasswordBox)sender).Password;

    private void OnProceedToLogin()
    {
        var loginWin = App.Services.GetRequiredService<LoginWindow>();
        loginWin.Show();
        Close();
    }
}
