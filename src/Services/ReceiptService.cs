using CassaEventiAI.Models;

namespace CassaEventiAI.Services;

/// <summary>
/// Composes the textual receipt structure for printing or preview.
/// Actual ESC/POS byte sending is in PrintingService.
/// </summary>
public class ReceiptService(ConfigService config)
{
    private const int ReceiptLineWidth = 38;
    private const int AmountWidth = 7;
    private const string BoldOn = "\x1B\x45\x01";
    private const string BoldOff = "\x1B\x45\x00";

    public ReceiptConfig GetConfig() => config.LoadReceiptConfig();

    public string BuildTextPreview(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
    {
        var cfg = config.LoadReceiptConfig();
        var lines = new List<string>();
        const string CutMark = "\x1E"; // section separator – PrintingService converts this to a real cut

        // ═════════════════════════════════════════════════════════════
        // SEZIONE 1: SCONTRINO FISCALE
        // ═════════════════════════════════════════════════════════════

        if (!string.IsNullOrWhiteSpace(cfg.HeaderText))
            lines.AddRange(WrapAndCenter(cfg.HeaderText, 40));

        lines.Add(string.Empty);
        lines.Add($"Scontrino #{sale.Id:D4}   {sale.CreatedAt:dd/MM/yy HH:mm}");
        lines.Add($"Operatore: {sale.OperatorName}");
        lines.Add(string.Empty);

        if (cfg.PrintPrices)
            lines.Add("Q.tà  Articolo          Prezzo   Totale");

        var items = filterDepts != null
            ? sale.Items.Where(i => filterDepts.Any(d => d.Id == i.DepartmentId))
            : sale.Items;

        foreach (var item in items)
        {
            var name = item.ProductName.Length > 15 ? item.ProductName[..15] : item.ProductName;
            var line = cfg.PrintPrices
                ? $"{item.Quantity,4}  {name, -15} {item.UnitPrice,7:F2}€ {item.LineTotal,7:F2}€"
                : $"{item.Quantity} {name}";
            lines.Add(line);
        }

        lines.Add(string.Empty);
        if (cfg.PrintPrices)
        {
            if (sale.DiscountPct > 0)
                lines.Add(FormatAmountLine($"Sconto {sale.DiscountPct:F0}%", -sale.Subtotal * sale.DiscountPct / 100));

            lines.Add(new string('-', 40));
            lines.Add(FormatAmountLine("TOTALE EUR", sale.Total));
            
            // if (sale.PaymentMethodKey == "cash")
            // {
            //     lines.Add(FormatAmountLine("Pagato", sale.CashGiven));
            //     lines.Add(FormatAmountLine("Resto", sale.Change));
            // }
        }

        lines.Add(string.Empty);
        if (!string.IsNullOrWhiteSpace(cfg.FooterText))
            lines.AddRange(WrapAndCenter(cfg.FooterText, 40));

        lines.Add(string.Empty);
        lines.Add(string.Empty);
        lines.Add(string.Empty);
        lines.Add(CutMark);

        // ═════════════════════════════════════════════════════════════
        // SEZIONI 2+: SCONTRINI PER REPARTO (solo se abilitato)
        // ═════════════════════════════════════════════════════════════

        if (cfg.PrintDepartmentSubtotals && allDepartments != null)
        {
            var deptGroups = sale.Items
                .GroupBy(i => new { i.DepartmentId, i.DepartmentName })
                .OrderBy(g => g.Key.DepartmentName);

            foreach (var deptGroup in deptGroups)
            {
                // Controlla se il reparto ha abilitato PrintSeparateReceipt
                var dept = allDepartments.FirstOrDefault(d => d.Id == deptGroup.Key.DepartmentId);
                if (dept == null || !dept.PrintSeparateReceipt)
                    continue;

                lines.Add(string.Empty);
                if (!string.IsNullOrWhiteSpace(cfg.HeaderText))
                    lines.AddRange(WrapAndCenter(cfg.HeaderText, 40));

                lines.Add(string.Empty);
                lines.Add($"Scontrino #{sale.Id:D4}   {sale.CreatedAt:dd/MM/yy HH:mm}");
                lines.Add($"Operatore: {sale.OperatorName}");
                lines.Add(string.Empty);
                lines.Add($"{BoldOn}{CenterText(deptGroup.Key.DepartmentName.ToUpperInvariant())}{BoldOff}");
                lines.Add(string.Empty);

                foreach (var item in deptGroup.OrderBy(i => i.ProductName))
                {
                    var name = item.ProductName.Length > 20 ? item.ProductName[..20] : item.ProductName;
                    lines.Add($"  {item.Quantity} {name}");
                }

                lines.Add(string.Empty);
                if (!string.IsNullOrWhiteSpace(cfg.FooterText))
                    lines.AddRange(WrapAndCenter(cfg.FooterText, 40));

                lines.Add(string.Empty);
                lines.Add(string.Empty);
                lines.Add(string.Empty);
                lines.Add(string.Empty);
                lines.Add(string.Empty);
                lines.Add(string.Empty);
                lines.Add(CutMark);
            }
        }

        return string.Join(Environment.NewLine, lines.Select(l => l == CutMark ? l : "   " + l));
    }

    private static string FormatAmountLine(string label, decimal amount)
    {
        var maxLabelWidth = ReceiptLineWidth - AmountWidth - 1;
        var normalizedLabel = label.Length > maxLabelWidth ? label[..maxLabelWidth] : label;
        return $"{normalizedLabel.PadRight(maxLabelWidth)} {amount,7:F2}€";
    }

    private static string CenterText(string text, int width = ReceiptLineWidth)
    {
        var clean = text.Length > width ? text[..width] : text;
        var leftPadding = Math.Max((width - clean.Length) / 2, 0);
        return new string(' ', leftPadding) + clean;
    }

    private static IEnumerable<string> WrapAndCenter(string text, int maxWidth)
    {
        foreach (var inputLine in text.Split('\n'))
        {
            var words = inputLine.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) { yield return string.Empty; continue; }

            var current = "";
            foreach (var word in words)
            {
                if (current.Length == 0)
                    current = word;
                else if (current.Length + 1 + word.Length <= maxWidth)
                    current += " " + word;
                else
                {
                    yield return CenterText(current, maxWidth);
                    current = word;
                }
            }
            if (current.Length > 0)
                yield return CenterText(current, maxWidth);
        }
    }
}
