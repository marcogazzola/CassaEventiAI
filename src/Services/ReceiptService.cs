using CplCassaEventi.Models;

namespace CplCassaEventi.Services;

/// <summary>
/// Composes the textual receipt structure for printing or preview.
/// Actual ESC/POS byte sending is in PrintingService.
/// </summary>
public class ReceiptService(ConfigService config)
{
    public ReceiptConfig GetConfig() => config.LoadReceiptConfig();

    public string BuildTextPreview(Sale sale, List<Department>? filterDepts = null)
    {
        var cfg = config.LoadReceiptConfig();
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(cfg.HeaderText))
        {
            lines.AddRange(cfg.HeaderText.Split('\n').Select(l => l.Trim()));
            lines.Add(new string('-', 32));
        }

        lines.Add($"Scontrino #{sale.Id:D4}  {sale.CreatedAt:dd/MM/yy HH:mm}");
        lines.Add($"Op: {sale.OperatorName}   Pagamento: {sale.PaymentMethodKey}");
        lines.Add(new string('-', 32));

        var items = filterDepts != null
            ? sale.Items.Where(i => filterDepts.Any(d => d.Id == i.DepartmentId))
            : sale.Items;

        foreach (var item in items)
        {
            var name = item.ProductName.Length > 18 ? item.ProductName[..18] : item.ProductName;
            var line = cfg.PrintPrices
                ? $"{item.Quantity}x {name}".PadRight(24) + $"{item.LineTotal,8:F2}"
                : $"{item.Quantity}x {name}";
            lines.Add(line);
        }

        lines.Add(new string('-', 32));
        if (cfg.PrintPrices)
        {
            if (sale.DiscountPct > 0)
                lines.Add($"Sconto {sale.DiscountPct:F0}%".PadRight(24) +
                          $"{-sale.Subtotal * sale.DiscountPct / 100,8:F2}");
            lines.Add($"TOTALE EUR".PadRight(22) + $"{sale.Total,10:F2}");
            if (sale.PaymentMethodKey == "cash")
            {
                lines.Add($"Pagato".PadRight(24) + $"{sale.CashGiven,8:F2}");
                lines.Add($"Resto".PadRight(24) + $"{sale.Change,8:F2}");
            }
        }

        if (!string.IsNullOrWhiteSpace(cfg.FooterText))
        {
            lines.Add(new string('-', 32));
            lines.AddRange(cfg.FooterText.Split('\n').Select(l => l.Trim()));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
