using CassaEventiAI.ViewModels;
using CassaEventiAI.Views.FrontOffice;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;

namespace CassaEventiAI.Views.BackOffice;

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

    private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender))
            return;

        if (ProductsTab.IsSelected)
            _vm.Products.Load();
    }

    private void PickDepartmentColor_Click(object sender, RoutedEventArgs e)
    {
        if (TryPickColor(_vm.Departments.EditColor, out var color))
            _vm.Departments.EditColor = color;
    }

    private static bool TryPickColor(string currentColor, out string hex)
    {
        using var dialog = new ColorDialog
        {
            AllowFullOpen = true,
            FullOpen = true,
            AnyColor = true
        };

        try
        {
            dialog.Color = System.Drawing.ColorTranslator.FromHtml(currentColor);
        }
        catch
        {
            dialog.Color = System.Drawing.ColorTranslator.FromHtml("#378ADD");
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            hex = currentColor;
            return false;
        }

        hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        return true;
    }
}
