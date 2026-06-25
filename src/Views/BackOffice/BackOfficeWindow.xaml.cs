using CassaEventiAI.ViewModels;
using CassaEventiAI.Models;
using CassaEventiAI.Views.FrontOffice;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CassaEventiAI.Views.BackOffice;

public partial class BackOfficeWindow : Window
{
    private readonly BackOfficeViewModel _vm;

    public BackOfficeWindow(BackOfficeViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        vm.EventArchived += OnNavigateToStartup;
        vm.EventRestored += OnNavigateToStartup;
    }

    public bool ShouldNavigateToStartup { get; private set; }

    private void OnNavigateToStartup()
    {
        ShouldNavigateToStartup = true;
        Close();
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

        // Lazy-load changelog quando il tab Info è selezionato
        if (e.AddedItems.Count > 0 && e.AddedItems[0] is TabItem tabItem && tabItem.Header?.ToString() == "Info")
        {
            _vm.LoadChangelog();
        }
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

    private void DepartmentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!TryGetClickedRow(e.OriginalSource, out _))
            return;

        if (((System.Windows.Controls.DataGrid)sender).SelectedItem is Department d && _vm.Departments.EditCommand.CanExecute(d))
            _vm.Departments.EditCommand.Execute(d);
    }

    private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!TryGetClickedRow(e.OriginalSource, out _))
            return;

        if (((System.Windows.Controls.DataGrid)sender).SelectedItem is Product p && _vm.Products.EditCommand.CanExecute(p))
            _vm.Products.EditCommand.Execute(p);
    }

    private void OperatorsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (!TryGetClickedRow(e.OriginalSource, out _))
            return;

        if (((System.Windows.Controls.DataGrid)sender).SelectedItem is Operator op && _vm.Operators.EditOperatorCommand.CanExecute(op))
            _vm.Operators.EditOperatorCommand.Execute(op);
    }

    private static bool TryGetClickedRow(object source, out DataGridRow? row)
    {
        row = null;
        var current = source as DependencyObject;
        while (current != null)
        {
            if (current is DataGridRow r)
            {
                row = r;
                return true;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        try
        {
            var psi = new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            System.Windows.MessageBox.Show($"Impossibile aprire il link: {e.Uri.AbsoluteUri}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        e.Handled = true;
    }
}
