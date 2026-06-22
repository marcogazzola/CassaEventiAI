using CassaEventiAI.Models;

namespace CassaEventiAI.Services;

/// <summary>
/// Composes the textual receipt structure for printing or preview.
/// Actual ESC/POS byte sending is in PrintingService.
/// </summary>
public class ReceiptService(ConfigService config)
{
    private const int ReceiptLineWidth = 27;
    private const int ReceiptBoldLineWidth = 20;
    private const int ReceiptSmallLineWidth = 38;
    private const int AmountWidth = 6;
    private const string BoldOn = "\x1B\x45\x01";
    private const string BoldOff = "\x1B\x45\x00";
    private const string SmallOn = "\x0E"; // marks line as 10pt (extra footer)
    private const string ReceiptDetaiRow = "\x1A\x50"; // marks line as 10pt (extra footer)

    public ReceiptConfig GetConfig() => config.LoadReceiptConfig();

    public string BuildTextPreview(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
    {
        var cfg = config.LoadReceiptConfig();
        var lines = new List<string>();
        const string CutMark = "\x1E"; // section separator – PrintingService converts this to a real cut

        // ═════════════════════════════════════════════════════════════
        // SEZIONE 1: SCONTRINO FISCALE
        // ═════════════════════════════════════════════════════════════

        if (cfg.PrintFiscalReceipt)
        {
            if (!string.IsNullOrWhiteSpace(cfg.HeaderText))
                lines.AddRange(WrapAndCenter(cfg.HeaderText, ReceiptLineWidth));

            lines.Add(string.Empty);
            lines.Add(CenterText($"Scontrino #{sale.Id:D4}"));
            if (cfg.PrintOperator)
                lines.Add($"Operatore: {sale.OperatorName}");
            lines.AddRange(WrapAndCenter($"{sale.CreatedAt:dd/MM/yy HH:mm}", ReceiptLineWidth));
            lines.Add(string.Empty);

            if (cfg.PrintPrices)
                lines.Add($"{"#",-2} {"Articolo",-18} {"Tot",AmountWidth}");
            lines.Add(string.Empty);

            var items = filterDepts != null
                ? sale.Items.Where(i => filterDepts.Any(d => d.Id == i.DepartmentId))
                : sale.Items;

            foreach (var item in items)
            {
                var name = item.ProductName.Length > 18 ? item.ProductName[..18] : item.ProductName;
                var line = cfg.PrintPrices
                    ? $"{ReceiptDetaiRow}{item.Quantity,2} {name,-18} {item.LineTotal,AmountWidth:F2}"
                    : $"{ReceiptDetaiRow}{item.Quantity} {name}";
                lines.Add(line);
            }

            lines.Add(string.Empty);
            if (cfg.PrintPrices)
            {
                if (sale.DiscountPct > 0)
                    lines.Add(FormatAmountLine($"Sconto {sale.DiscountPct:F0}%", -sale.Subtotal * sale.DiscountPct / 100));

                lines.Add(new string('-', ReceiptLineWidth));
                lines.Add(FormatAmountLine("TOTALE", sale.Total));
            }

            lines.Add(string.Empty);
            lines.Add(string.Empty);
            if (!string.IsNullOrWhiteSpace(cfg.FooterText))
                lines.AddRange(WrapAndCenter(cfg.FooterText, ReceiptLineWidth));

            if (cfg.ExtraFooterEnabled && !string.IsNullOrWhiteSpace(cfg.ExtraFooterText))
            {
                lines.Add(string.Empty);
                lines.AddRange(WrapAndCenter(cfg.ExtraFooterText, ReceiptSmallLineWidth).Select(l => SmallOn + l));
            }

            lines.Add(CutMark);
        }

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
                    lines.AddRange(WrapAndCenter(cfg.HeaderText, ReceiptLineWidth));

                lines.Add(string.Empty);
                lines.Add(CenterText($"Scontrino #{sale.Id:D4}"));
                if (cfg.PrintOperator)
                    lines.Add($"Operatore: {sale.OperatorName}");
                lines.AddRange(WrapAndCenter($"{sale.CreatedAt:dd/MM/yy HH:mm}", ReceiptLineWidth));
                lines.Add(string.Empty);
                lines.Add($"{BoldOn}{(CenterText($" **** {deptGroup.Key.DepartmentName.ToUpperInvariant()} **** ", ReceiptBoldLineWidth))}{BoldOff}");
                lines.Add(string.Empty);

                foreach (var item in deptGroup.OrderBy(i => i.ProductName))
                {
                    var name = item.ProductName.Length > 23 ? item.ProductName[..23] : item.ProductName;
                    lines.Add(CenterText($"{item.Quantity,3} {name}", ReceiptLineWidth));
                }

                lines.Add(string.Empty);
                lines.Add(string.Empty);
                if (!string.IsNullOrWhiteSpace(cfg.FooterText))
                    lines.AddRange(WrapAndCenter(cfg.FooterText, ReceiptLineWidth));

                if (cfg.ExtraFooterEnabled && !cfg.ExtraFooterOnlyFirst && !string.IsNullOrWhiteSpace(cfg.ExtraFooterText))
                {
                    lines.Add(string.Empty);
                    lines.AddRange(WrapAndCenter(cfg.ExtraFooterText, ReceiptSmallLineWidth).Select(l => SmallOn + l));
                }

                // lines.Add(string.Empty);
                // lines.Add(string.Empty);
                lines.Add(CutMark);
            }
        }

        return string.Join(Environment.NewLine, lines.Select(l => l == CutMark ? l:(l.Contains(ReceiptDetaiRow)?l.Replace(ReceiptDetaiRow, "") : " " + l)));
    }

    private static string FormatAmountLine(string label, decimal amount)
    {
        var maxLabelWidth = ReceiptLineWidth - AmountWidth - 2;
        var normalizedLabel = label.Length > maxLabelWidth ? label[..maxLabelWidth] : label;
        return $"{normalizedLabel.PadRight(maxLabelWidth)} {amount,(AmountWidth):F2}€";
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
