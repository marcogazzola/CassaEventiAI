using CplCassaEventi.Services;
using CplCassaEventi.ViewModels;
using CplCassaEventi.Views.Shared;
using CplCassaEventi.Views.BackOffice;
using CplCassaEventi.Views.Reports;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CplCassaEventi.Views.FrontOffice;

public partial class MainWindow : Window
{
    private readonly FrontOfficeViewModel _vm;
    private readonly SaleService _sales;

    public MainWindow(FrontOfficeViewModel vm, SaleService sales)
    {
        InitializeComponent();
        _vm = vm;
        _sales = sales;
        DataContext = vm;
        vm.ShiftClosed += OnShiftClosed;

        _ = InitShiftAsync();
    }

    private async Task InitShiftAsync()
    {
        var auth = App.Services.GetRequiredService<AuthService>();
        var existingShift = await _sales.GetOpenShiftAsync(auth.CurrentOperator!.Id);
        int shiftId;
        if (existingShift != null) shiftId = existingShift.Id;
        else shiftId = (await _sales.OpenShiftAsync()).Id;
        _vm.SetShift(shiftId);
    }

    private void OpenBackOffice_Click(object sender, RoutedEventArgs e)
    {
        var auth = App.Services.GetRequiredService<AuthService>();
        if (!auth.IsAdmin)
        {
            System.Windows.MessageBox.Show("Accesso negato. Solo gli amministratori possono accedere al BackOffice.",
                "Accesso negato", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var bo = App.Services.GetRequiredService<BackOfficeWindow>();
        bo.Owner = this;
        bo.ShowDialog();
        _vm.ReloadProductsCommand.Execute(null);
    }

    private void OpenReports_Click(object sender, RoutedEventArgs e)
    {
        var auth = App.Services.GetRequiredService<AuthService>();
        if (!auth.IsAdmin)
        {
            System.Windows.MessageBox.Show("Accesso negato. Solo gli amministratori possono accedere alla reportistica.",
                "Accesso negato", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rw = App.Services.GetRequiredService<ReportWindow>();
        rw.Owner = this;
        rw.ShowDialog();
    }

    private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is not System.Windows.Controls.TextBox box)
            return;
        var normalized = e.Text.Replace(',', '.');
        e.Handled = !decimal.TryParse($"{box.Text}{normalized}", System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    private void NumericTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(typeof(string)))
        {
            e.CancelCommand();
            return;
        }
        var text = (string)e.DataObject.GetData(typeof(string))!;
        var normalized = text.Replace(',', '.');
        if (!decimal.TryParse(normalized, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            e.CancelCommand();
        }
    }

    private void OnShiftClosed()
    {
        Dispatcher.Invoke(() =>
        {
            App.Services.GetRequiredService<AuthService>().Logout();
            var startup = App.Services.GetRequiredService<StartupWindow>();
            startup.Show();
            Close();
        });
    }
}
