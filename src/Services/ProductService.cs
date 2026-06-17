using CplCassaEventi.Models;

namespace CplCassaEventi.Services;

public class ProductService(ConfigService config)
{
    private List<Product>? _products;
    private List<Department>? _departments;

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

    public bool DecrementStock(int productId, int qty)
    {
        var list = config.LoadProducts();
        var p = list.FirstOrDefault(x => x.Id == productId);
        if (p == null || !p.TrackStock) return true;
        if (p.StockQty < qty) return false;
        p.StockQty -= qty;
        config.SaveProducts(list);
        return true;
    }
}
