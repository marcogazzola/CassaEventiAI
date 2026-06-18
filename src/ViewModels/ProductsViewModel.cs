using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CassaEventiAI.Models;
using CassaEventiAI.Services;
using System.Collections.ObjectModel;

namespace CassaEventiAI.ViewModels;

public partial class ProductsViewModel(ProductService products) : BaseViewModel
{
    [ObservableProperty] private ObservableCollection<Product> _allProducts = [];
    [ObservableProperty] private ObservableCollection<Product> _filteredProducts = [];
    [ObservableProperty] private ObservableCollection<Department> _departments = [];
    [ObservableProperty] private Department? _filterDept;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private Product? _selected;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isNew;

    // Edit form
    [ObservableProperty] private string _editName = string.Empty;
    [ObservableProperty] private decimal _editPrice;
    [ObservableProperty] private int _editDeptId;
    [ObservableProperty] private bool _editIsActive = true;
    [ObservableProperty] private bool _editTrackStock;
    [ObservableProperty] private int _editStockQty;
    [ObservableProperty] private int _editSortOrder;

    partial void OnFilterDeptChanged(Department? value) => ApplyFilter();
    partial void OnSearchTextChanged(string value) => ApplyFilter();

    public void Load()
    {
        var depts = products.GetDepartments(activeOnly: false).ToList();
        depts.Insert(0, new Department { Id = 0, Name = "— Tutti i reparti —", Color = "#CCCCCC" });
        Departments = new(depts);
        AllProducts = new(products.GetProducts(activeOnly: false));
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var q = AllProducts.AsEnumerable();
        if (FilterDept != null && FilterDept.Id > 0)
            q = q.Where(p => p.DepartmentId == FilterDept.Id);
        if (!string.IsNullOrWhiteSpace(SearchText))
            q = q.Where(p => p.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        FilteredProducts = new(q);
    }

    public string DeptName(int id) => Departments.FirstOrDefault(d => d.Id == id)?.Name ?? "—";

    [RelayCommand] private void New()
    {
        var defaultDepartment = Departments.FirstOrDefault();
        Selected = null; EditName = ""; EditPrice = 0; EditIsActive = true;
        EditDeptId = defaultDepartment?.Id ?? 0; EditTrackStock = false;
        EditStockQty = 0; EditSortOrder = AllProducts.Count; IsNew = true; IsEditing = true;
    }

    [RelayCommand] private void Edit(Product p)
    {
        Selected = p; EditName = p.Name; EditPrice = p.Price;
        EditDeptId = p.DepartmentId; EditIsActive = p.IsActive;
        EditTrackStock = p.TrackStock; EditStockQty = p.StockQty;
        EditSortOrder = p.SortOrder; IsNew = false; IsEditing = true;
    }

    [RelayCommand] private void Cancel() { IsEditing = false; }

    [RelayCommand] private void Save()
    {
        if (string.IsNullOrWhiteSpace(EditName)) { StatusMessage = "Nome obbligatorio."; return; }
        if (EditPrice < 0) { StatusMessage = "Prezzo non valido."; return; }
        if (EditDeptId == 0) { StatusMessage = "Seleziona un reparto."; return; }
        var p = IsNew ? new Product() : (Selected ?? new Product());
        p.Name = EditName.Trim(); p.Price = EditPrice;
        p.DepartmentId = EditDeptId; p.IsActive = EditIsActive;
        p.TrackStock = EditTrackStock; p.StockQty = EditStockQty; p.SortOrder = EditSortOrder;
        products.SaveProduct(p);
        Load(); IsEditing = false; StatusMessage = $"Articolo '{p.Name}' salvato.";
    }

    [RelayCommand] private void Delete(Product p)
    {
        if (!Confirm($"Eliminare l'articolo '{p.Name}'?")) return;
        products.DeleteProduct(p.Id);
        Load(); StatusMessage = $"Articolo '{p.Name}' eliminato.";
    }

    [RelayCommand] private void ToggleActive(Product p)
    {
        p.IsActive = !p.IsActive;
        products.SaveProduct(p);
        Load(); StatusMessage = $"'{p.Name}' {(p.IsActive ? "abilitato" : "disabilitato")}.";
    }
}
