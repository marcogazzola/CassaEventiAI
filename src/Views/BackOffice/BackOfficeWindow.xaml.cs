using CplCassaEventi.ViewModels;
using CplCassaEventi.Views.FrontOffice;
using System.Windows;
using System.Windows.Controls;

namespace CplCassaEventi.Views.BackOffice;

public partial class BackOfficeWindow : Window
{
    private readonly BackOfficeViewModel _vm;

    public BackOfficeWindow(BackOfficeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    // Operator edit password boxes — bridge to ViewModel
    private void OpPwdBox_PasswordChanged(object s, RoutedEventArgs e)
        => _vm.Operators.EditNewPassword = ((PasswordBox)s).Password;

    private void OpConfirmPwdBox_PasswordChanged(object s, RoutedEventArgs e)
        => _vm.Operators.EditConfirmPassword = ((PasswordBox)s).Password;
}
