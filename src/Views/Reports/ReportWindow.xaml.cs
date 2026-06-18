using CplCassaEventi.ViewModels;
using System.Windows;

namespace CplCassaEventi.Views.Reports;

public partial class ReportWindow : Window
{
    private readonly ReportViewModel _vm;

    public ReportWindow(ReportViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _vm.RefreshCommand.ExecuteAsync(null);
    }
}
