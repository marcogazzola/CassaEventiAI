using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CplCassaEventi.Models;
using CplCassaEventi.Services;
using System.Collections.ObjectModel;

namespace CplCassaEventi.ViewModels;

public partial class FrontOfficeViewModel : BaseViewModel
{
    private readonly SaleService _sales;
    private readonly ProductService _products;
    private readonly PrintingService _printing;
    private readonly ConfigService _config;
    private int? _currentShiftId;

    public FrontOfficeViewModel(
        SaleService sales, ProductService products,
        PrintingService printing, ConfigService config)
    {
        _sales = sales; _products = products;
        _printing = printing; _config = config;
        ReloadProducts();
        _ = RefreshStatsAsync();
    }

    // ── Products & departments ─────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<Department> _departments = [];
    [ObservableProperty] private ObservableCollection<Product> _allProducts = [];
    [ObservableProperty] private ObservableCollection<Product> _filteredProducts = [];
    [ObservableProperty] private Department? _selectedDepartment;

    partial void OnSelectedDepartmentChanged(Department? value) => ApplyDeptFilter();

    [RelayCommand]
    public void ReloadProducts()
    {
        var depts = _products.GetDepartments();
        var prods = _products.GetProducts();
        Departments = new(depts);
        AllProducts = new(prods);
        ApplyDeptFilter();
        PaymentMethods = new(_config.LoadPaymentMethods().Where(p => p.IsActive));
        SelectedPaymentMethod = PaymentMethods.FirstOrDefault();
    }

    [RelayCommand]
    private void SelectDept(Department? d)
    {
        SelectedDepartment = d;
        ApplyDeptFilter();
    }

    [RelayCommand]
    private void ClearDeptFilter()
    {
        SelectedDepartment = null;
        ApplyDeptFilter();
    }

    private void ApplyDeptFilter()
        => FilteredProducts = SelectedDepartment == null
            ? new(AllProducts)
            : new(AllProducts.Where(p => p.DepartmentId == SelectedDepartment.Id));

    // ── Cart ──────────────────────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<CartItem> _cartItems = [];
    [ObservableProperty] private decimal _discountPct;
    [ObservableProperty] private decimal _subtotal;
    [ObservableProperty] private decimal _total;
    [ObservableProperty] private decimal _cashGiven;
    [ObservableProperty] private decimal _change;

    partial void OnDiscountPctChanged(decimal _) => RecalcTotals();
    partial void OnCashGivenChanged(decimal v)
    {
        if (SelectedPaymentMethod?.RequiresCashInput == true)
            Change = Math.Max(0, v - Total);
    }

    [RelayCommand]
    public void AddProduct(Product p)
    {
        var existing = CartItems.FirstOrDefault(i => i.ProductId == p.Id);
        if (existing != null) { existing.Quantity++; OnPropertyChanged(nameof(CartItems)); }
        else
        {
            var dept = Departments.FirstOrDefault(d => d.Id == p.DepartmentId);
            CartItems.Add(new CartItem
            {
                ProductId = p.Id, ProductName = p.Name,
                DepartmentId = p.DepartmentId, DepartmentName = dept?.Name ?? "",
                UnitPrice = p.Price
            });
        }
        RecalcTotals();
    }

    [RelayCommand]
    public void IncreaseQty(CartItem item) { item.Quantity++; OnPropertyChanged(nameof(CartItems)); RecalcTotals(); }

    [RelayCommand]
    public void DecreaseQty(CartItem item)
    {
        if (item.Quantity <= 1) CartItems.Remove(item);
        else { item.Quantity--; OnPropertyChanged(nameof(CartItems)); }
        RecalcTotals();
    }

    [RelayCommand]
    public void RemoveItem(CartItem item) { CartItems.Remove(item); RecalcTotals(); }

    [RelayCommand]
    public void ClearCart()
    {
        CartItems.Clear(); DiscountPct = 0; CashGiven = 0; Change = 0;
        RecalcTotals();
    }

    private void RecalcTotals()
    {
        Subtotal = CartItems.Sum(i => i.LineTotal);
        Total = Math.Round(Subtotal * (1 - DiscountPct / 100m), 2);
        if (SelectedPaymentMethod?.RequiresCashInput == true)
            Change = Math.Max(0, CashGiven - Total);
    }

    // ── Payment methods ───────────────────────────────────────────────────

    [ObservableProperty] private ObservableCollection<PaymentMethod> _paymentMethods = [];
    [ObservableProperty] private PaymentMethod? _selectedPaymentMethod;
    [ObservableProperty] private bool _showCashInput;

    partial void OnSelectedPaymentMethodChanged(PaymentMethod? v)
    {
        ShowCashInput = v?.RequiresCashInput == true;
        if (!ShowCashInput) { CashGiven = 0; Change = 0; }
        else Change = Math.Max(0, CashGiven - Total);
    }

    [RelayCommand]
    private void SelectPayment(PaymentMethod pm) => SelectedPaymentMethod = pm;

    // ── Checkout ──────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task CheckoutAsync()
    {
        if (CartItems.Count == 0) { ShowError("Il carrello è vuoto."); return; }
        if (SelectedPaymentMethod == null) { ShowError("Seleziona un metodo di pagamento."); return; }
        if (SelectedPaymentMethod.RequiresCashInput && CashGiven < Total)
            { ShowError($"Importo contanti insufficiente (mancano € {Total - CashGiven:F2})."); return; }

        IsBusy = true;
        try
        {
            var result = await _sales.CompleteSaleAsync(
                CartItems, DiscountPct,
                SelectedPaymentMethod.Key, SelectedPaymentMethod.Label,
                CashGiven, _currentShiftId);

            if (!result.Success) { ShowError(result.Error ?? "Errore durante la vendita."); return; }

            var sale = await _sales.GetSaleByIdAsync(result.SaleId);
            if (sale != null) _printing.PrintSale(sale);

            ClearCart();
            await RefreshStatsAsync();
            StatusMessage = $"Scontrino #{result.SaleId} emesso.";
        }
        finally { IsBusy = false; }
    }

    // ── Void / Reprint ────────────────────────────────────────────────────

    [ObservableProperty] private bool _voidPanelVisible;
    [ObservableProperty] private string _voidSaleIdText = string.Empty;
    [ObservableProperty] private string _voidReason = string.Empty;

    [RelayCommand]
    private void OpenVoidPanel() => VoidPanelVisible = true;

    [RelayCommand]
    private void CloseVoidPanel() { VoidPanelVisible = false; VoidSaleIdText = ""; VoidReason = ""; }

    [RelayCommand]
    private async Task ConfirmVoidAsync()
    {
        if (!int.TryParse(VoidSaleIdText, out var id)) { ShowError("ID scontrino non valido."); return; }
        if (string.IsNullOrWhiteSpace(VoidReason)) { ShowError("Inserisci il motivo dello storno."); return; }
        if (!Confirm($"Annullare lo scontrino #{id}?\nMotivo: {VoidReason}")) return;

        var ok = await _sales.VoidSaleAsync(id, VoidReason);
        if (ok)
        {
            StatusMessage = $"Scontrino #{id} annullato.";
            CloseVoidPanel();
            await RefreshStatsAsync();
        }
        else ShowError($"Scontrino #{id} non trovato o già annullato.");
    }

    [RelayCommand]
    private async Task ReprintLastAsync()
    {
        var sale = await _sales.GetLastSaleAsync();
        if (sale == null) { ShowError("Nessuno scontrino da ristampare."); return; }
        _printing.ReprintLast(sale);
        StatusMessage = $"Scontrino #{sale.Id} ristampato.";
    }

    // ── Footer stats ──────────────────────────────────────────────────────

    [ObservableProperty] private int _salesCount;
    [ObservableProperty] private decimal _totalIncassato;
    [ObservableProperty] private decimal _lastOrderAmount;
    [ObservableProperty] private bool _showTotalInFooter;

    private async Task RefreshStatsAsync()
    {
        var (count, total, last) = await _sales.GetSessionStatsAsync(_currentShiftId);
        SalesCount = count;
        TotalIncassato = total;
        LastOrderAmount = last;
        ShowTotalInFooter = App.CurrentSettings.ShowTotalInFooter;
    }

    // ── Shift ─────────────────────────────────────────────────────────────

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
    }
}
