using CplCassaEventi.Models;

namespace CplCassaEventi.Services;

/// <summary>
/// Composes the textual receipt structure for printing or preview.
/// Actual ESC/POS byte sending is in PrintingService.
/// </summary>
public class ReceiptService(ConfigService config)
{
    public ReceiptConfig GetConfig() => config.LoadReceiptConfig();

    public string BuildTextPreview(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
    {
        var cfg = config.LoadReceiptConfig();
        var lines = new List<string>();
        const string PAPER_CUT = "\x1D\x56\x41"; // ESC/POS thermal paper cut command

        // ═════════════════════════════════════════════════════════════
        // SEZIONE 1: SCONTRINO FISCALE
        // ═════════════════════════════════════════════════════════════

        if (!string.IsNullOrWhiteSpace(cfg.HeaderText))
            lines.AddRange(cfg.HeaderText.Split('\n').Select(l => l.Trim()));

        lines.Add(string.Empty);
        lines.Add($"Scontrino #{sale.Id:D4}   {sale.CreatedAt:dd/MM/yy HH:mm}");
        lines.Add($"Operatore: {sale.OperatorName}");
        lines.Add(string.Empty);

        if (cfg.PrintPrices)
            lines.Add("Q.tà  Articolo         Prezzo    Totale");

        var items = filterDepts != null
            ? sale.Items.Where(i => filterDepts.Any(d => d.Id == i.DepartmentId))
            : sale.Items;

        foreach (var item in items)
        {
            var name = item.ProductName.Length > 15 ? item.ProductName[..15] : item.ProductName;
            var line = cfg.PrintPrices
                ? $"{item.Quantity,3}  {name,-15} {item.UnitPrice,7:F2}€ {item.LineTotal,7:F2}€"
                : $"{item.Quantity} {name}";
            lines.Add(line);
        }

        lines.Add(string.Empty);
        if (cfg.PrintPrices)
        {
            if (sale.DiscountPct > 0)
                lines.Add($"Sconto {sale.DiscountPct:F0}%                 {-sale.Subtotal * sale.DiscountPct / 100,7:F2}€");
            
            lines.Add($"TOTALE EUR                   {sale.Total,7:F2}€");
            
            if (sale.PaymentMethodKey == "cash")
            {
                lines.Add($"Pagato                       {sale.CashGiven,7:F2}€");
                lines.Add($"Resto                        {sale.Change,7:F2}€");
            }
        }

        lines.Add(string.Empty);
        if (!string.IsNullOrWhiteSpace(cfg.FooterText))
            lines.AddRange(cfg.FooterText.Split('\n').Select(l => l.Trim()));

        lines.Add(string.Empty);
        lines.Add(PAPER_CUT);

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
                    lines.AddRange(cfg.HeaderText.Split('\n').Select(l => l.Trim()));

                lines.Add(string.Empty);
                lines.Add($"Scontrino #{sale.Id:D4}   {sale.CreatedAt:dd/MM/yy HH:mm}");
                lines.Add($"Operatore: {sale.OperatorName}");
                lines.Add(string.Empty);
                lines.Add($"[{deptGroup.Key.DepartmentName}]");
                lines.Add(string.Empty);

                foreach (var item in deptGroup.OrderBy(i => i.ProductName))
                {
                    var name = item.ProductName.Length > 20 ? item.ProductName[..20] : item.ProductName;
                    lines.Add($"  {item.Quantity} {name}");
                }

                lines.Add(string.Empty);
                if (!string.IsNullOrWhiteSpace(cfg.FooterText))
                    lines.AddRange(cfg.FooterText.Split('\n').Select(l => l.Trim()));

                lines.Add(string.Empty);
                lines.Add(PAPER_CUT);
            }
        }

        return string.Join(Environment.NewLine, lines);
    }

}
