using CassaEventiAI.Models;

namespace CassaEventiAI.Services;

public class ProductService(ConfigService config)
{
    private List<Product>? _products;
    private List<Department>? _departments;
    private static readonly object _stockLock = new();

    public List<Product> GetProducts(bool activeOnly = true)
    {
        _products = config.LoadProducts();
        return activeOnly ? _products.Where(p => p.IsActive).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList()
                          : _products.OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList();
    }

    public List<Department> GetDepartments(bool activeOnly = true)
    {
        _departments = config.LoadDepartments();
        return activeOnly ? _departments.Where(d => d.IsActive).OrderBy(d => d.SortOrder).ThenBy(d => d.Name).ToList()
                          : _departments.OrderBy(d => d.SortOrder).ThenBy(d => d.Name).ToList();
    }

    public void SaveProduct(Product p)
    {
        var list = config.LoadProducts();
        var existing = list.FirstOrDefault(x => x.Id == p.Id);
        if (existing != null) list[list.IndexOf(existing)] = p;
        else { p.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1; list.Add(p); }
        config.SaveProducts(list);
        _products = null;
    }

    public void DeleteProduct(int id)
    {
        var list = config.LoadProducts();
        list.RemoveAll(p => p.Id == id);
        config.SaveProducts(list);
        _products = null;
    }

    public void SaveDepartment(Department d)
    {
        var list = config.LoadDepartments();
        var existing = list.FirstOrDefault(x => x.Id == d.Id);
        if (existing != null) list[list.IndexOf(existing)] = d;
        else { d.Id = list.Count > 0 ? list.Max(x => x.Id) + 1 : 1; list.Add(d); }
        config.SaveDepartments(list);
        _departments = null;
    }

    public void DeleteDepartment(int id)
    {
        var list = config.LoadDepartments();
        list.RemoveAll(d => d.Id == id);
        config.SaveDepartments(list);
        _departments = null;
    }

    public bool TryReserveStock(IEnumerable<(int ProductId, int Quantity)> items, out string? error)
    {
        lock (_stockLock)
        {
            var list = config.LoadProducts();
            foreach (var (productId, quantity) in items)
            {
                var product = list.FirstOrDefault(x => x.Id == productId);
                if (product == null || !product.TrackStock)
                    continue;
                if (quantity <= 0)
                    continue;
                if (product.StockQty < quantity)
                {
                    error = $"Stock insufficiente per '{product.Name}'. Disponibili: {product.StockQty}.";
                    return false;
                }
            }

            foreach (var (productId, quantity) in items)
            {
                var product = list.FirstOrDefault(x => x.Id == productId);
                if (product == null || !product.TrackStock)
                    continue;
                if (quantity <= 0)
                    continue;
                product.StockQty -= quantity;
            }
            config.SaveProducts(list);
            error = null;
            _products = null;
            return true;
        }
    }

    public void RestoreStock(IEnumerable<(int ProductId, int Quantity)> items)
    {
        lock (_stockLock)
        {
            var list = config.LoadProducts();
            foreach (var (productId, quantity) in items)
            {
                if (quantity <= 0)
                    continue;
                var product = list.FirstOrDefault(x => x.Id == productId);
                if (product == null || !product.TrackStock)
                    continue;
                product.StockQty += quantity;
            }
            config.SaveProducts(list);
            _products = null;
        }
    }

    public bool CanAddToCart(int productId, int requestedQty)
    {
        var list = config.LoadProducts();
        var product = list.FirstOrDefault(x => x.Id == productId);
        if (product == null || !product.TrackStock)
            return true;
        return requestedQty <= product.StockQty;
    }

    public string? GetDepartmentColor(int departmentId)
    {
        var departments = _departments ?? config.LoadDepartments();
        return departments.FirstOrDefault(d => d.Id == departmentId)?.Color;
    }

    public List<ProductGroup> GetProductGroupsByDepartment(bool activeOnly = true)
    {
        var departments = GetDepartments(activeOnly);
        var products = GetProducts(activeOnly);
        return departments
            .Select(d => new ProductGroup
            {
                DepartmentId = d.Id,
                DepartmentName = d.Name,
                DepartmentColor = string.IsNullOrWhiteSpace(d.Color) ? "#378ADD" : d.Color,
                Products = products.Where(p => p.DepartmentId == d.Id).OrderBy(p => p.SortOrder).ThenBy(p => p.Name).ToList()
            })
            .Where(g => g.Products.Count > 0)
            .ToList();
    }

    public Product? GetProductById(int productId)
    {
        var list = config.LoadProducts();
        return list.FirstOrDefault(p => p.Id == productId);
    }

    public Dictionary<int, Product> GetProductMap()
    {
        return config.LoadProducts().ToDictionary(p => p.Id, p => p);
    }

    public bool DecrementStock(int productId, int qty)
    {
        if (qty <= 0)
            return true;
        return TryReserveStock([(productId, qty)], out _);
    }
}
