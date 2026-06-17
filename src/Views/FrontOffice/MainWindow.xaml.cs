using CplCassaEventi.Services;
using CplCassaEventi.ViewModels;
using CplCassaEventi.Views.BackOffice;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

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
            MessageBox.Show("Accesso negato. Solo gli amministratori possono accedere al BackOffice.",
                "Accesso negato", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var bo = App.Services.GetRequiredService<BackOfficeWindow>();
        bo.Owner = this;
        bo.ShowDialog();
        _vm.ReloadProductsCommand.Execute(null);
    }
}
