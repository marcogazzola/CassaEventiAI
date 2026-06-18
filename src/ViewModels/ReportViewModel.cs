using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CplCassaEventi.Models;
using CplCassaEventi.Services;
using System.Collections.ObjectModel;

namespace CplCassaEventi.ViewModels;

public partial class ReportViewModel : BaseViewModel
{
    private readonly ReportService _report;
    private readonly SaleService _sales;
    private readonly PrintingService _printing;
    private readonly ProductService _products;

    [ObservableProperty] private DateTime _fromDate = DateTime.Today;
    [ObservableProperty] private DateTime _toDate = DateTime.Today;
    [ObservableProperty] private int _issuedReceipts;
    [ObservableProperty] private int _voidedReceipts;
    [ObservableProperty] private decimal _totalSold;
    [ObservableProperty] private ObservableCollection<DailyProductSalesRow> _dailyProducts = [];
    [ObservableProperty] private ObservableCollection<DailyOrderRow> _dailyOrders = [];
    [ObservableProperty] private DailyOrderRow? _selectedOrder;
    [ObservableProperty] private ObservableCollection<SaleItem> _selectedOrderItems = [];
    [ObservableProperty] private string _selectedOrderPreview = string.Empty;

    public ReportViewModel(ReportService report, SaleService sales, PrintingService printing, ProductService products)
    {
        _report = report;
        _sales = sales;
        _printing = printing;
        _products = products;
    }

    partial void OnSelectedOrderChanged(DailyOrderRow? value)
    {
        _ = LoadSelectedOrderDetailAsync(value);
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var startOfDay = FromDate.Date;
            var endOfDay = ToDate.Date.AddDays(1).AddTicks(-1);

            var cash = await _report.GetDailyCashReportAsync(startOfDay, endOfDay);
            IssuedReceipts = cash.IssuedReceipts;
            VoidedReceipts = cash.VoidedReceipts;
            TotalSold = cash.TotalSold;
            DailyProducts = new(cash.Products);

            var orders = await _report.GetDailyOrdersAsync(startOfDay, endOfDay);
            DailyOrders = new(orders);
            SelectedOrder = DailyOrders.FirstOrDefault();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Excel|*.xlsx",
            FileName = $"incasso_{FromDate:yyyyMMdd}_a_{ToDate:yyyyMMdd}.xlsx",
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog() != true)
            return;

        var startOfDay = FromDate.Date;
        var endOfDay = ToDate.Date.AddDays(1).AddTicks(-1);
        var report = await _report.GetDailyCashReportAsync(startOfDay, endOfDay);
        await _report.ExportDailyCashExcelAsync(dialog.FileName, report);
        StatusMessage = $"Esportato: {System.IO.Path.GetFileName(dialog.FileName)}";
    }

    [RelayCommand]
    private async Task PrintSelectedPreviewAsync()
    {
        if (SelectedOrder == null || string.IsNullOrWhiteSpace(SelectedOrderPreview))
            return;
        _printing.PrintRawPreview(SelectedOrderPreview);
        await Task.CompletedTask;
    }

    private async Task LoadSelectedOrderDetailAsync(DailyOrderRow? row)
    {
        if (row == null)
        {
            SelectedOrderItems = [];
            SelectedOrderPreview = string.Empty;
            return;
        }

        var sale = await _sales.GetSaleByIdAsync(row.SaleId);
        if (sale == null)
        {
            SelectedOrderItems = [];
            SelectedOrderPreview = string.Empty;
            return;
        }

        SelectedOrderItems = new(sale.Items.OrderBy(i => i.DepartmentName).ThenBy(i => i.ProductName));
        var departments = _products.GetDepartments();
        SelectedOrderPreview = _printing.BuildSalePreview(sale, null, departments);
    }
}
