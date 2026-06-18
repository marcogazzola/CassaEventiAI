using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CplCassaEventi.Models;

// ── DB Entities ───────────────────────────────────────────────────────────

public class Sale
{
    [Key] public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public string PaymentMethodKey { get; set; } = "cash";
    public string PaymentMethodLabel { get; set; } = "Contanti";
    public decimal CashGiven { get; set; }
    public decimal Change { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public bool IsPrinted { get; set; }
    public bool IsVoided { get; set; }
    public string? VoidReason { get; set; }
    public int? ShiftId { get; set; }
    public ICollection<SaleItem> Items { get; set; } = [];
}

public class SaleItem
{
    [Key] public int Id { get; set; }
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    [ForeignKey(nameof(SaleId))] public Sale? Sale { get; set; }
}

public class OperatorShift
{
    [Key] public int Id { get; set; }
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }
    public int SalesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsClosed { get; set; }
}

// ── JSON Config Models ─────────────────────────────────────────────────────

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#378ADD";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool PrintSeparateReceipt { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;
    public bool TrackStock { get; set; }
    public int StockQty { get; set; }
    public int SortOrder { get; set; }
}

public class PaymentMethod
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool RequiresCashInput { get; set; }
    public int SortOrder { get; set; }
}

public class Operator
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "cashier";   // "cashier" | "admin"
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; } = true;
}

public class ReceiptConfig
{
    public string HeaderText { get; set; } = string.Empty;
    public string FooterText { get; set; } = string.Empty;
    public bool PrintPrices { get; set; } = true;
    public bool PrintDepartmentSubtotals { get; set; }
    public int CopiesCount { get; set; } = 1;
}

public class AppSettings
{
    public string ActiveDbPath { get; set; } = string.Empty;
    public string ActiveEventName { get; set; } = string.Empty;
    public string PrinterName { get; set; } = string.Empty;
    public bool PrinterEnabled { get; set; } = true;
    public bool ShowTotalInFooter { get; set; } = true;
    public bool KioskMode { get; set; }
    public bool AutoBackupEnabled { get; set; } = true;
    public int AutoBackupIntervalMinutes { get; set; } = 30;
    public string ArchiveFolderPath { get; set; } = string.Empty;
    public string DataFolderPath { get; set; } = string.Empty;
}

// ── View/Transfer Models ───────────────────────────────────────────────────

public partial class CartItem : ObservableObject
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    [ObservableProperty]
    private int _quantity = 1;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(LineTotal));

    public decimal LineTotal => UnitPrice * Quantity;
}

public class SaleResult
{
    public bool Success { get; set; }
    public int SaleId { get; set; }
    public string? Error { get; set; }
}

public class ShiftSummary
{
    public int SalesCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime StartedAt { get; set; }
    public TimeSpan Duration { get; set; }
    public Dictionary<string, decimal> TotalByPaymentMethod { get; set; } = [];
}

public class ProductGroup
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string DepartmentColor { get; set; } = "#378ADD";
    public List<Product> Products { get; set; } = [];
}

public record SaleLookupRow(int SaleId, DateTime CreatedAt, string OperatorName, decimal Total, bool IsVoided = false);

public record DailyProductSalesRow(string ProductName, int Quantity, decimal TotalAmount);

public record DailyOrderRow(int SaleId, DateTime CreatedAt, string OperatorName, string PaymentMethod, decimal Total, bool IsVoided);
