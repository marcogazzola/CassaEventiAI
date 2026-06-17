using CplCassaEventi.Models;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using System.IO;
using System.IO.Ports;

namespace CplCassaEventi.Services;

public class PrintingService(ConfigService config)
{
    private SerialPrinter? _printer;
    private readonly EPSON _e = new();

    private void EnsurePrinter()
    {
        var settings = App.CurrentSettings;
        if (!settings.PrinterEnabled) return;
        if (_printer != null) return;

        try
        {
            _printer = new SerialPrinter(settings.PrinterPort, 9600);
        }
        catch
        {
            // Printer not available — will silently skip
        }
    }

    public void PrintSale(Sale sale, List<Department>? filterDepts = null)
    {
        EnsurePrinter();
        var cfg = config.LoadReceiptConfig();
        var bytes = BuildReceipt(sale, cfg, filterDepts);
        Send(bytes);
    }

    public void ReprintLast(Sale sale) => PrintSale(sale);

    public void PrintTestPage()
    {
        EnsurePrinter();
        var bytes = ByteSplicer.Combine(
            _e.Initialize(),
            _e.CenterAlign(),
            _e.SetStyles(PrintStyle.Bold),
            _e.Print("=== TEST STAMPA ==="),
            _e.PrintLine(""),
            _e.LeftAlign(),
            _e.PrintLine($"Porta: {App.CurrentSettings.PrinterPort}"),
            _e.PrintLine($"Data: {DateTime.Now:dd/MM/yyyy HH:mm}"),
            _e.PrintLine(""),
            _e.FullCut()
        );
        Send(bytes);
    }

    private byte[] BuildReceipt(Sale sale, ReceiptConfig cfg, List<Department>? filterDepts)
    {
        var parts = new List<byte[]>();

        parts.Add(_e.Initialize());
        parts.Add(_e.CenterAlign());

        if (!string.IsNullOrWhiteSpace(cfg.HeaderText))
        {
            foreach (var line in cfg.HeaderText.Split('\n'))
            {
                parts.Add(_e.SetStyles(PrintStyle.Bold));
                parts.Add(_e.PrintLine(line.Trim()));
            }
            parts.Add(_e.SetStyles(PrintStyle.None));
        }

        parts.Add(_e.PrintLine($"Scontrino #{sale.Id:D4}"));
        parts.Add(_e.PrintLine(sale.CreatedAt.ToString("dd/MM/yyyy HH:mm")));
        parts.Add(_e.PrintLine($"Operatore: {sale.OperatorName}"));
        parts.Add(_e.PrintLine(new string('-', 32)));
        parts.Add(_e.LeftAlign());

        var items = filterDepts != null
            ? sale.Items.Where(i => filterDepts.Any(d => d.Id == i.DepartmentId)).ToList()
            : sale.Items.ToList();

        foreach (var item in items)
        {
            var name = item.ProductName.Length > 18 ? item.ProductName[..18] : item.ProductName;
            if (cfg.PrintPrices)
            {
                var line = $"{item.Quantity}x {name}".PadRight(24) +
                           $"{item.LineTotal,8:F2}";
                parts.Add(_e.PrintLine(line));
            }
            else
            {
                parts.Add(_e.PrintLine($"{item.Quantity}x {name}"));
            }
        }

        parts.Add(_e.PrintLine(new string('-', 32)));

        if (cfg.PrintPrices)
        {
            if (sale.DiscountPct > 0)
            {
                parts.Add(_e.PrintLine($"Subtotale".PadRight(24) + $"{sale.Subtotal,8:F2}"));
                parts.Add(_e.PrintLine($"Sconto {sale.DiscountPct:F0}%".PadRight(24) +
                                       $"{-sale.Subtotal * sale.DiscountPct / 100,8:F2}"));
            }

            parts.Add(_e.SetStyles(PrintStyle.Bold));
            parts.Add(_e.PrintLine($"TOTALE EUR".PadRight(22) + $"{sale.Total,10:F2}"));
            parts.Add(_e.SetStyles(PrintStyle.None));

            if (sale.PaymentMethodKey == "cash")
            {
                parts.Add(_e.PrintLine($"Pagato".PadRight(24) + $"{sale.CashGiven,8:F2}"));
                parts.Add(_e.PrintLine($"Resto".PadRight(24) + $"{sale.Change,8:F2}"));
            }
            else
            {
                parts.Add(_e.PrintLine($"Pagamento: {sale.PaymentMethodKey}"));
            }
        }

        if (!string.IsNullOrWhiteSpace(cfg.FooterText))
        {
            parts.Add(_e.PrintLine(new string('-', 32)));
            parts.Add(_e.CenterAlign());
            foreach (var line in cfg.FooterText.Split('\n'))
                parts.Add(_e.PrintLine(line.Trim()));
        }

        parts.Add(_e.PrintLine(""));
        parts.Add(_e.FullCut());

        return ByteSplicer.Combine([.. parts]);
    }

    private void Send(byte[] data)
    {
        try { _printer?.Write(data); }
        catch { /* Log or swallow — printer failure must not block the sale */ }
    }

    public void Dispose() => _printer?.Dispose();
}
