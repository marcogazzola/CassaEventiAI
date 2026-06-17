using CplCassaEventi.Data;
using CplCassaEventi.Models;
using Microsoft.EntityFrameworkCore;

namespace CplCassaEventi.Services;

public class SaleService(EventService eventService, AuthService auth)
{
    private CassaDbContext Db => eventService.Db;

    // ── Sales ─────────────────────────────────────────────────────────────

    public async Task<SaleResult> CompleteSaleAsync(
        IEnumerable<CartItem> items,
        decimal discountPct,
        string paymentMethodKey,
        string paymentMethodLabel,
        decimal cashGiven,
        int? shiftId = null)
    {
        var list = items.ToList();
        if (list.Count == 0) return new SaleResult { Error = "Carrello vuoto." };

        var subtotal = list.Sum(i => i.LineTotal);
        var total = subtotal * (1 - discountPct / 100m);
        var change = paymentMethodKey == "cash" ? Math.Max(0, cashGiven - total) : 0;

        var sale = new Sale
        {
            CreatedAt = DateTime.Now,
            OperatorId = auth.CurrentOperator?.Id ?? 0,
            OperatorName = auth.CurrentOperator?.DisplayName ?? "—",
            PaymentMethodKey = paymentMethodKey,
            PaymentMethodLabel = paymentMethodLabel,
            CashGiven = cashGiven,
            Change = change,
            DiscountPct = discountPct,
            Subtotal = subtotal,
            Total = total,
            ShiftId = shiftId,
            Items = list.Select(i => new SaleItem
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

        Db.Sales.Add(sale);
        await Db.SaveChangesAsync();
        return new SaleResult { Success = true, SaleId = sale.Id };
    }

    public async Task<Sale?> GetSaleByIdAsync(int id)
        => await Db.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<Sale?> GetLastSaleAsync()
        => await Db.Sales.Include(s => s.Items)
            .Where(s => !s.IsVoided)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

    public async Task<bool> VoidSaleAsync(int saleId, string reason)
    {
        var sale = await Db.Sales.FindAsync(saleId);
        if (sale == null || sale.IsVoided) return false;
        sale.IsVoided = true;
        sale.VoidReason = reason;
        await Db.SaveChangesAsync();
        return true;
    }

    // ── Session stats ─────────────────────────────────────────────────────

    public async Task<(int Count, decimal Total, decimal LastAmount)> GetSessionStatsAsync(int? shiftId = null)
    {
        var q = Db.Sales.Where(s => !s.IsVoided);
        if (shiftId.HasValue) q = q.Where(s => s.ShiftId == shiftId);
        var count = await q.CountAsync();
        var total = count > 0 ? await q.SumAsync(s => s.Total) : 0;
        var last  = await q.OrderByDescending(s => s.Id).Select(s => (decimal?)s.Total).FirstOrDefaultAsync();
        return (count, total, last ?? 0);
    }

    // ── Shifts ────────────────────────────────────────────────────────────

    public async Task<OperatorShift> OpenShiftAsync()
    {
        var shift = new OperatorShift
        {
            OperatorId = auth.CurrentOperator!.Id,
            OperatorName = auth.CurrentOperator.DisplayName,
            StartedAt = DateTime.Now
        };
        Db.OperatorShifts.Add(shift);
        await Db.SaveChangesAsync();
        return shift;
    }

    public async Task<ShiftSummary> CloseShiftAsync(int shiftId)
    {
        var shift = await Db.OperatorShifts.FindAsync(shiftId)
            ?? throw new InvalidOperationException("Turno non trovato.");

        var sales = await Db.Sales.Where(s => s.ShiftId == shiftId && !s.IsVoided).ToListAsync();
        shift.SalesCount = sales.Count;
        shift.TotalAmount = sales.Sum(s => s.Total);
        shift.ClosedAt = DateTime.Now;
        shift.IsClosed = true;
        await Db.SaveChangesAsync();

        return new ShiftSummary
        {
            SalesCount = shift.SalesCount,
            TotalAmount = shift.TotalAmount,
            StartedAt = shift.StartedAt,
            Duration = DateTime.Now - shift.StartedAt,
            TotalByPaymentMethod = sales
                .GroupBy(s => s.PaymentMethodLabel)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.Total))
        };
    }

    public async Task<OperatorShift?> GetOpenShiftAsync(int operatorId)
        => await Db.OperatorShifts
            .Where(s => s.OperatorId == operatorId && !s.IsClosed)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();
}
