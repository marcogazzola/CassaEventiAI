using CplCassaEventi.Data;
using CplCassaEventi.Models;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;

namespace CplCassaEventi.Services;

public record ProductReportRow(string Reparto, string Articolo, int Qty, decimal PrezzoUnit, decimal Totale);
public record DeptReportRow(string Reparto, int Qty, decimal Totale);
public record PaymentReportRow(string MetodoPagamento, int NumScontrini, decimal Totale);
public record ShiftReportRow(string Operatore, DateTime Inizio, DateTime? Fine, int Scontrini, decimal Totale);

public class ReportService(EventService eventService)
{
    private CassaDbContext Db => eventService.Db;

    public async Task<List<ProductReportRow>> GetProductReportAsync(DateTime? from = null, DateTime? to = null)
    {
        var q = Db.SaleItems.Include(i => i.Sale).Where(i => !i.Sale!.IsVoided);
        if (from.HasValue) q = q.Where(i => i.Sale!.CreatedAt >= from);
        if (to.HasValue)   q = q.Where(i => i.Sale!.CreatedAt <= to);
        return await q
            .GroupBy(i => new { i.DepartmentName, i.ProductName, i.UnitPrice })
            .ToListAsync()
            .ContinueWith(t => t.Result.OrderBy(g => g.Key.DepartmentName)
            .ThenBy(g => g.Key.ProductName)
            .Select(g => new ProductReportRow(g.Key.DepartmentName, g.Key.ProductName,
                g.Sum(i => i.Quantity), g.Key.UnitPrice, g.Sum(i => i.LineTotal)))
            .ToList());

    }

    public async Task<List<DeptReportRow>> GetDeptReportAsync(DateTime? from = null, DateTime? to = null)
    {
        var q = Db.SaleItems.Include(i => i.Sale).Where(i => !i.Sale!.IsVoided);
        if (from.HasValue) q = q.Where(i => i.Sale!.CreatedAt >= from);
        if (to.HasValue)   q = q.Where(i => i.Sale!.CreatedAt <= to);
        return await q
            .GroupBy(i => i.DepartmentName)
            .Select(g => new DeptReportRow(g.Key, g.Sum(i => i.Quantity), g.Sum(i => i.LineTotal)))
            .ToListAsync()
            .ContinueWith(t => t.Result.OrderByDescending(r => r.Totale).ToList());
    }

    public async Task<List<PaymentReportRow>> GetPaymentReportAsync(DateTime? from = null, DateTime? to = null)
    {
        var q = Db.Sales.Where(s => !s.IsVoided);
        if (from.HasValue) q = q.Where(s => s.CreatedAt >= from);
        if (to.HasValue)   q = q.Where(s => s.CreatedAt <= to);
        return await q
            .GroupBy(s => s.PaymentMethodLabel)
            .Select(g => new PaymentReportRow(g.Key, g.Count(), g.Sum(s => s.Total)))
            .ToListAsync();
    }

    public async Task<List<ShiftReportRow>> GetShiftReportAsync()
        => await Db.OperatorShifts
            .Select(s => new ShiftReportRow(s.OperatorName, s.StartedAt, s.ClosedAt, s.SalesCount, s.TotalAmount))
            .ToListAsync()
            .ContinueWith(t => t.Result.OrderByDescending(s => s.Inizio).ToList());

    public async Task<(int Count, decimal Total)> GetTotalsAsync(DateTime? from = null, DateTime? to = null)
    {
        var q = Db.Sales.Where(s => !s.IsVoided);
        if (from.HasValue) q = q.Where(s => s.CreatedAt >= from);
        if (to.HasValue)   q = q.Where(s => s.CreatedAt <= to);
        var count = await q.CountAsync();
        var total = count > 0 ? await q.SumAsync(s => s.Total) : 0;
        return (count, total);
    }

    public async Task ExportProductCsvAsync(string filePath, DateTime? from = null, DateTime? to = null)
    {
        var rows = await GetProductReportAsync(from, to);
        await using var writer = new StreamWriter(filePath, false, System.Text.Encoding.UTF8);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(rows);
    }
}
