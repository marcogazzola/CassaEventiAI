using CassaEventiAI.Models;
using System.Drawing;
using System.Drawing.Printing;

namespace CassaEventiAI.Services;

public class PrintingService(ReceiptService receiptService)
{
    public List<string> GetInstalledPrinters()
        => PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(x => x).ToList();

    public string BuildSalePreview(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
        => receiptService.BuildTextPreview(sale, filterDepts, allDepartments);

    public void PrintSale(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
    {
        if (!App.CurrentSettings.PrinterEnabled)
            return;
        var text = BuildSalePreview(sale, filterDepts, allDepartments);
        PrintText(text);
    }

    public void ReprintLast(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null) 
        => PrintSale(sale, filterDepts, allDepartments);

    public void PrintTestPage()
    {
        if (!App.CurrentSettings.PrinterEnabled)
            return;
        var text = string.Join(Environment.NewLine,
            "=== TEST STAMPA ===",
            $"Stampante: {App.CurrentSettings.PrinterName}",
            $"Data: {DateTime.Now:dd/MM/yyyy HH:mm}",
            string.Empty);
        PrintText(text);
    }

    public void PrintRawPreview(string preview)
    {
        if (!App.CurrentSettings.PrinterEnabled)
            return;
        PrintText(preview);
    }

    private void PrintText(string text)
    {
        var printerName = App.CurrentSettings.PrinterName;
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("Nessuna stampante configurata.");

        using var doc = new PrintDocument();
        doc.PrinterSettings.PrinterName = printerName;
        if (!doc.PrinterSettings.IsValid)
            throw new InvalidOperationException($"Stampante non valida: {printerName}");

        var lines = text.Replace("\r", string.Empty).Split('\n');
        var index = 0;
        using var font = new Font("Consolas", 9, FontStyle.Regular, GraphicsUnit.Point);
        var lineHeight = font.GetHeight() + 1;

        doc.PrintPage += (_, args) =>
        {
            if (args.Graphics == null)
                throw new InvalidOperationException("Contesto grafico stampante non disponibile.");

            var y = (float)args.MarginBounds.Top;
            while (index < lines.Length)
            {
                if (y + lineHeight > args.MarginBounds.Bottom)
                {
                    args.HasMorePages = true;
                    return;
                }
                args.Graphics.DrawString(lines[index], font, Brushes.Black, args.MarginBounds.Left, y);
                index++;
                y += lineHeight;
            }
            args.HasMorePages = false;
        };

        doc.Print();
    }
}
