using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CassaEventiAI.Models;
using CassaEventiAI.Services;
using CassaEventiAI.Views.Shared;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace CassaEventiAI.ViewModels;

public partial class FrontOfficeViewModel : BaseViewModel
{
    private readonly SaleService _sales;
    private readonly ProductService _products;
    private readonly PrintingService _printing;
    private readonly ConfigService _config;
    private readonly AuthService _auth;
    private readonly DispatcherTimer _clockTimer;
    private int? _currentShiftId;
    public event Action? ShiftClosed;
    public event Action<Sale, List<Department>>? SaleCompleted;
    public event Action<string>? PreviewRequested;

    public FrontOfficeViewModel(
        SaleService sales, ProductService products,
        PrintingService printing, ConfigService config, AuthService auth)
    {
        _sales = sales;
        _products = products;
        _printing = printing;
        _config = config;
        _auth = auth;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => ToolbarDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        ToolbarDateTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        IsAdmin = _auth.IsAdmin;
        ConnectedUserName = _auth.CurrentOperator?.DisplayName ?? "—";
        _clockTimer.Start();

        ReloadProducts();
        _ = RefreshStatsAsync();
    }

    [ObservableProperty] private ObservableCollection<Department> _departments = [];
    [ObservableProperty] private ObservableCollection<ProductGroup> _groupedProducts = [];
    [ObservableProperty] private ObservableCollection<Product> _allProducts = [];
    [ObservableProperty] private ObservableCollection<CartItem> _cartItems = [];
    [ObservableProperty] private decimal _discountPct;
    [ObservableProperty] private decimal _subtotal;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private int _cartQuantityCount;
    [ObservableProperty] private decimal _cashGiven;
    [ObservableProperty] private decimal _change;
    [ObservableProperty] private ObservableCollection<PaymentMethod> _paymentMethods = [];
    [ObservableProperty] private PaymentMethod? _selectedPaymentMethod;
    [ObservableProperty] private bool _showCashInput;
    [ObservableProperty] private decimal _discountAmount;
    [ObservableProperty] private bool _isDiscountActive;
    [ObservableProperty] private int _salesCount;
    [ObservableProperty] private decimal _totalIncassato;
    [ObservableProperty] private decimal _lastOrderAmount;
    [ObservableProperty] private bool _showTotalInFooter;
    [ObservableProperty] private bool _isAdmin;
    [ObservableProperty] private string _connectedUserName = string.Empty;
    [ObservableProperty] private string _toolbarDateTime = string.Empty;
    [ObservableProperty] private bool _voidPanelVisible;
    [ObservableProperty] private string _voidReason = string.Empty;
    [ObservableProperty] private string _voidSearchText = string.Empty;
    [ObservableProperty] private ObservableCollection<SaleLookupRow> _voidSearchResults = [];
    [ObservableProperty] private SaleLookupRow? _selectedVoidSale;

    partial void OnDiscountPctChanged(decimal value)
    {
        DiscountPct = Math.Round(Math.Clamp(value, 0m, 100m), 2);
        RecalcTotals();
    }

    partial void OnCashGivenChanged(decimal value)
    {
        CashGiven = Math.Round(Math.Clamp(value, 0m, 1000m), 2);
        if (SelectedPaymentMethod?.RequiresCashInput == true)
            Change = Math.Max(0, CashGiven - Total);
    }

    partial void OnSelectedPaymentMethodChanged(PaymentMethod? value)
    {
        ShowCashInput = value?.RequiresCashInput == true;
        if (!ShowCashInput)
        {
            CashGiven = 0;
            Change = 0;
        }
        else
        {
            Change = Math.Max(0, CashGiven - Total);
        }
    }

    [RelayCommand]
    public void ReloadProducts()
    {
        var selectedPaymentKey = SelectedPaymentMethod?.Key;
        var depts = _products.GetDepartments();
        var groups = _products.GetProductGroupsByDepartment();
        var products = _products.GetProducts();
        Departments = new(depts);
        GroupedProducts = new(groups);
        AllProducts = new(products);
        PaymentMethods = new(_config.LoadPaymentMethods().Where(p => p.IsActive));
        SelectedPaymentMethod = PaymentMethods.FirstOrDefault(p => p.Key == selectedPaymentKey)
            ?? PaymentMethods.FirstOrDefault();
    }

    [RelayCommand]
    public void AddProduct(Product p)
    {
        var existing = CartItems.FirstOrDefault(i => i.ProductId == p.Id);
        var requestedQty = (existing?.Quantity ?? 0) + 1;
        if (!_products.CanAddToCart(p.Id, requestedQty))
        {
            ShowError("Stock insufficiente per questo articolo.");
            return;
        }

        if (existing != null)
        {
            existing.Quantity++;
        }
        else
        {
            var dept = Departments.FirstOrDefault(d => d.Id == p.DepartmentId);
            CartItems.Add(new CartItem
            {
                ProductId = p.Id,
                ProductName = p.Name,
                DepartmentId = p.DepartmentId,
                DepartmentName = dept?.Name ?? string.Empty,
                UnitPrice = p.Price
            });
        }
        RecalcTotals();
    }

    [RelayCommand]
    public void IncreaseQty(CartItem item)
    {
        var requestedQty = item.Quantity + 1;
        if (!_products.CanAddToCart(item.ProductId, requestedQty))
        {
            ShowError("Stock insufficiente per questo articolo.");
            return;
        }
        item.Quantity++;
        RecalcTotals();
    }

    [RelayCommand]
    public void DecreaseQty(CartItem item)
    {
        if (item.Quantity <= 1) CartItems.Remove(item);
        else item.Quantity--;
        RecalcTotals();
    }

    [RelayCommand]
    public void ClearCart()
    {
        CartItems.Clear();
        DiscountPct = 0;
        CashGiven = 0;
        Change = 0;
        RecalcTotals();
    }

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (CartItems.Count == 0) { ShowError("Il carrello è vuoto."); return; }
        if (SelectedPaymentMethod == null) { ShowError("Seleziona un metodo di pagamento."); return; }
        if (SelectedPaymentMethod.RequiresCashInput && CashGiven > 0 && CashGiven < Total)
        {
            ShowError($"Importo contanti insufficiente (mancano € {Total - CashGiven:F2}).");
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _sales.CompleteSaleAsync(
                CartItems, DiscountPct,
                SelectedPaymentMethod.Key, SelectedPaymentMethod.Label,
                CashGiven, _currentShiftId);

            if (!result.Success) { ShowError(result.Error ?? "Errore durante la vendita."); return; }

            var sale = await _sales.GetSaleByIdAsync(result.SaleId);
            var depts = Departments.ToList();
            if (sale != null)
                _printing.PrintSale(sale, null, depts);

            ClearCart();
            ReloadProducts();
            await RefreshStatsAsync();
            StatusMessage = $"Scontrino #{result.SaleId} emesso.";

            if (App.CurrentSettings.ShowOrderSummary && sale != null)
                SaleCompleted?.Invoke(sale, depts);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private void SelectPayment(PaymentMethod pm) => SelectedPaymentMethod = pm;

    [RelayCommand]
    private async Task ReprintLastAsync()
    {
        var sale = await _sales.GetLastSaleAsync();
        if (sale == null) { ShowError("Nessuno scontrino da ristampare."); return; }
        _printing.ReprintLast(sale, null, Departments.ToList());
        StatusMessage = $"Scontrino #{sale.Id} ristampato.";
    }

    [RelayCommand]
    private void PreviewCurrentReceipt()
    {
        if (CartItems.Count == 0)
        {
            ShowError("Il carrello è vuoto.");
            return;
        }

        var previewSale = new Sale
        {
            Id = 0,
            CreatedAt = DateTime.Now,
            OperatorName = "ANTEPRIMA",
            PaymentMethodKey = SelectedPaymentMethod?.Key ?? "cash",
            PaymentMethodLabel = SelectedPaymentMethod?.Label ?? "Contanti",
            CashGiven = CashGiven,
            Change = Change,
            DiscountPct = DiscountPct,
            Subtotal = Subtotal,
            Total = Total,
            Items = CartItems.Select(i => new SaleItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                DepartmentId = i.DepartmentId,
                DepartmentName = i.DepartmentName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };

        var preview = _printing.BuildSalePreview(previewSale, null, Departments.ToList());
        PreviewRequested?.Invoke(preview);
    }

    [RelayCommand]
    private async Task OpenVoidPanelAsync()
    {
        VoidReason = string.Empty;
        VoidSearchText = string.Empty;
        SelectedVoidSale = null;
        VoidPanelVisible = true;
        await SearchVoidSalesAsync();
    }

    [RelayCommand]
    private void CloseVoidPanel()
    {
        VoidPanelVisible = false;
        VoidReason = string.Empty;
        VoidSearchText = string.Empty;
        SelectedVoidSale = null;
        VoidSearchResults = [];
    }

    [RelayCommand]
    private async Task SearchVoidSalesAsync()
    {
        var rows = await _sales.SearchIssuedSalesAsync(VoidSearchText);
        VoidSearchResults = new(rows);
        if (SelectedVoidSale == null)
            SelectedVoidSale = VoidSearchResults.FirstOrDefault();
    }

    [RelayCommand]
    private void SelectVoidSale(SaleLookupRow row) => SelectedVoidSale = row;

    [RelayCommand]
    private async Task ConfirmVoidAsync()
    {
        if (SelectedVoidSale == null) { ShowError("Seleziona uno scontrino."); return; }

        if (SelectedVoidSale.IsVoided)
        {
            // Riattiva uno scontrino stornato
            if (!Confirm($"Riattivare lo scontrino #{SelectedVoidSale.SaleId}?")) return;
            var ok = await _sales.ReactivateSaleAsync(SelectedVoidSale.SaleId);
            if (!ok)
            {
                ShowError($"Impossibile riattivare lo scontrino #{SelectedVoidSale.SaleId}.");
                return;
            }
            StatusMessage = $"Scontrino #{SelectedVoidSale.SaleId} riattivato.";
            VoidReason = string.Empty;
        }
        else
        {
            // Annulla uno scontrino attivo
            if (string.IsNullOrWhiteSpace(VoidReason)) { ShowError("Inserisci il motivo dello storno."); return; }
            if (!Confirm($"Annullare lo scontrino #{SelectedVoidSale.SaleId}?\nMotivo: {VoidReason}")) return;

            var ok = await _sales.VoidSaleAsync(SelectedVoidSale.SaleId, VoidReason);
            if (!ok)
            {
                ShowError($"Scontrino #{SelectedVoidSale.SaleId} non trovato o già annullato.");
                return;
            }
            StatusMessage = $"Scontrino #{SelectedVoidSale.SaleId} annullato.";
        }

        CloseVoidPanel();
        ReloadProducts();
        await RefreshStatsAsync();
    }

    public void SetShift(int shiftId) => _currentShiftId = shiftId;

    [RelayCommand]
    private async Task CloseShiftAsync()
    {
        if (_currentShiftId == null) { ShowError("Nessun turno aperto."); return; }
        if (!Confirm("Chiudere il turno corrente?")) return;
        var summary = await _sales.CloseShiftAsync(_currentShiftId.Value);
        _currentShiftId = null;
        var byMethod = string.Join(", ", summary.TotalByPaymentMethod.Select(kv => $"{kv.Key}: €{kv.Value:F2}"));
        StatusMessage = $"Turno chiuso · {summary.SalesCount} scontrini · €{summary.TotalAmount:F2} · {byMethod}";
        await RefreshStatsAsync();
        ShiftClosed?.Invoke();
    }

    private void RecalcTotals()
    {
        CartQuantityCount = CartItems.Sum(i => i.Quantity);
        Subtotal = CartItems.Sum(i => i.LineTotal);
        DiscountAmount = Math.Round(Subtotal * DiscountPct / 100m, 2);
        IsDiscountActive = DiscountPct > 0;
        Total = Math.Round(Subtotal * (1 - DiscountPct / 100m), 2);
        if (SelectedPaymentMethod?.RequiresCashInput == true)
            Change = Math.Max(0, CashGiven - Total);
    }

    private async Task RefreshStatsAsync()
    {
        var (count, total, last) = await _sales.GetSessionStatsAsync(_currentShiftId);
        SalesCount = count;
        TotalIncassato = total;
        LastOrderAmount = last;
        ShowTotalInFooter = _auth.IsAdmin || App.CurrentSettings.ShowTotalInFooter;
    }
}
