using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CplCassaEventi.Services;
using System.Collections.ObjectModel;

namespace CplCassaEventi.ViewModels;

public partial class ReportViewModel(ReportService report) : BaseViewModel
{
    [ObservableProperty] private DateTime _from = DateTime.Today;
    [ObservableProperty] private DateTime _to = DateTime.Today;
    [ObservableProperty] private ObservableCollection<ProductReportRow> _productRows = [];
    [ObservableProperty] private ObservableCollection<DeptReportRow> _deptRows = [];
    [ObservableProperty] private ObservableCollection<PaymentReportRow> _paymentRows = [];
    [ObservableProperty] private ObservableCollection<ShiftReportRow> _shiftRows = [];
    [ObservableProperty] private int _totalSales;
    [ObservableProperty] private decimal _grandTotal;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var dateFrom = From.Date;
            var dateTo = To.Date.AddDays(1).AddTicks(-1);
            ProductRows  = new(await report.GetProductReportAsync(dateFrom, dateTo));
            DeptRows     = new(await report.GetDeptReportAsync(dateFrom, dateTo));
            PaymentRows  = new(await report.GetPaymentReportAsync(dateFrom, dateTo));
            ShiftRows    = new(await report.GetShiftReportAsync());
            var (count, total) = await report.GetTotalsAsync(dateFrom, dateTo);
            TotalSales = count; GrandTotal = total;
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "CSV|*.csv",
            FileName = $"report_{DateTime.Now:yyyyMMdd}.csv",
            DefaultExt = ".csv"
        };
        if (dlg.ShowDialog() != true) return;
        var dateFrom = From.Date;
        var dateTo = To.Date.AddDays(1).AddTicks(-1);
        await report.ExportProductCsvAsync(dlg.FileName, dateFrom, dateTo);
        StatusMessage = $"Esportato: {System.IO.Path.GetFileName(dlg.FileName)}";
    }

    [RelayCommand] private void SetToday() { From = To = DateTime.Today; }
    [RelayCommand] private void SetAllTime() { From = new DateTime(2020, 1, 1); To = DateTime.Today; }
}
