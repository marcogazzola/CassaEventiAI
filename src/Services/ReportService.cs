using CassaEventiAI.Data;
using CassaEventiAI.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CassaEventiAI.Services;

public record DailyCashReport(
    DateTime StartDate,
    DateTime EndDate,
    int IssuedReceipts,
    int VoidedReceipts,
    decimal TotalSold,
    decimal TotalDiscounts,
    List<DailyProductSalesRow> Products);

public class ReportService(EventService eventService)
{
    private CassaDbContext Db => eventService.Db;

    public async Task<DailyCashReport> GetDailyCashReportAsync(DateTime startDate, DateTime endDate)
    {
        var sales = await Db.Sales
            .Include(s => s.Items)
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .ToListAsync();

        var activeSales = sales.Where(s => !s.IsVoided).ToList();
        var products = activeSales
            .SelectMany(s => s.Items.Select(i => new { s.CreatedAt, Item = i }))
            .GroupBy(x => new { x.CreatedAt.Date, x.Item.ProductName, x.Item.UnitPrice })
            .OrderBy(g => g.Key.Date).ThenBy(g => g.Key.ProductName)
            .Select(g => new DailyProductSalesRow(
                g.Key.Date,
                g.Key.ProductName,
                g.Key.UnitPrice,
                g.Sum(x => x.Item.Quantity),
                g.Sum(x => x.Item.LineTotal)))
            .ToList();

        return new DailyCashReport(
            startDate,
            endDate,
            sales.Count,
            sales.Count(s => s.IsVoided),
            activeSales.Sum(s => s.Total),
            activeSales.Sum(s => s.Subtotal - s.Total),
            products);
    }

    public async Task<List<DailyOrderRow>> GetDailyOrdersAsync(DateTime startDate, DateTime endDate)
    {
        return await Db.Sales
            .Where(s => s.CreatedAt >= startDate && s.CreatedAt <= endDate)
            .OrderByDescending(s => s.Id)
            .Select(s => new DailyOrderRow(
                s.Id,
                s.CreatedAt,
                s.OperatorName,
                s.PaymentMethodLabel,
                s.Total,
                s.IsVoided,
                s.Subtotal - s.Total))
            .ToListAsync()
            .ContinueWith(t => t.Result.OrderByDescending(o => o.SaleId).ToList());
    }

    public async Task ExportDailyCashExcelAsync(string filePath, DailyCashReport report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Incasso");

        ws.Cell(1, 1).Value = "Periodo";
        ws.Cell(1, 2).Value = $"{report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}";
        ws.Cell(2, 1).Value = "Scontrini emessi";
        ws.Cell(2, 2).Value = report.IssuedReceipts;
        ws.Cell(3, 1).Value = "Scontrini annullati";
        ws.Cell(3, 2).Value = report.VoidedReceipts;
        ws.Cell(4, 1).Value = "Totale venduto";
        ws.Cell(4, 2).Value = report.TotalSold;
        ws.Cell(4, 2).Style.NumberFormat.Format = "€ #,##0.00";
        ws.Cell(5, 1).Value = "Sconti applicati";
        ws.Cell(5, 2).Value = report.TotalDiscounts;
        ws.Cell(5, 2).Style.NumberFormat.Format = "€ #,##0.00";

        ws.Cell(7, 1).Value = "Giorno";
        ws.Cell(7, 2).Value = "Prodotto";
        ws.Cell(7, 3).Value = "Prezzo";
        ws.Cell(7, 4).Value = "Quantità";
        ws.Cell(7, 5).Value = "Importo totale";
        ws.Range(7, 1, 7, 5).Style.Font.Bold = true;

        var row = 8;
        foreach (var product in report.Products)
        {
            ws.Cell(row, 1).Value = product.Date.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = product.ProductName;
            ws.Cell(row, 3).Value = product.UnitPrice;
            ws.Cell(row, 3).Style.NumberFormat.Format = "€ #,##0.00";
            ws.Cell(row, 4).Value = product.Quantity;
            ws.Cell(row, 5).Value = product.TotalAmount;
            ws.Cell(row, 5).Style.NumberFormat.Format = "€ #,##0.00";
            row++;
        }

        ws.Columns().AdjustToContents();
        workbook.SaveAs(filePath);
        await Task.CompletedTask;
    }
}
