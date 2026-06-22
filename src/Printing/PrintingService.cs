using CassaEventiAI.Models;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace CassaEventiAI.Services;

public class PrintingService(ReceiptService receiptService)
{
    // ESC/POS – solo taglio carta (il testo viene stampato via GDI)
    private static readonly byte[] GsCut = [0x1D, 0x56, 0x30]; // GS V 0 – taglio completo (POS-80)

    private const string ReceiptFontName  = "Lucida Console";
    private const float ReceiptFontSize  = 11f;
    private const float ReceiptTitleSize = 14f;
    private const float ReceiptSmallSize = 8f;

    public List<string> GetInstalledPrinters()
        => PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(x => x).ToList();

    public string BuildSalePreview(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null, bool isPreview = true)
    {
        var receipt = receiptService.BuildTextPreview(sale, filterDepts, allDepartments);
        if (isPreview)
            receipt = receipt.Replace("\x1E", "\n--- TAGLIO ---\n");
        return receipt;
    }

    public void PrintSale(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
    {
        if (!App.CurrentSettings.PrinterEnabled)
            return;
        PrintText(BuildSalePreview(sale, filterDepts, allDepartments, false));
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
            "Simbolo euro: € 1,00",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty) + "\x1E";
        PrintText(text);
    }

    public void PrintRawPreview(string preview)
    {
        if (!App.CurrentSettings.PrinterEnabled)
            return;
        PrintText(preview);
    }

    private static void PrintText(string text)
    {
        var printerName = App.CurrentSettings.PrinterName;
        if (string.IsNullOrWhiteSpace(printerName))
            throw new InvalidOperationException("Nessuna stampante configurata.");

        var settings = new PrinterSettings { PrinterName = printerName };
        if (!settings.IsValid)
            throw new InvalidOperationException($"Stampante non valida: {printerName}");

        // Split on the section-cut marker; each section is printed via GDI + raw cut
        var sections = text.Split('\x1E', StringSplitOptions.RemoveEmptyEntries);
        foreach (var section in sections)
        {
            var normalized = section.TrimStart('\r', '\n');
            if (string.IsNullOrWhiteSpace(normalized))
                continue;

            PrintGdi(normalized, printerName);
        }
    }

    private static void PrintGdi(string text, string printerName)
    {
        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');

        using var baseFont  = new Font(ReceiptFontName, ReceiptFontSize,  FontStyle.Regular, GraphicsUnit.Point);
        using var boldFont  = new Font(ReceiptFontName, ReceiptTitleSize, FontStyle.Bold,    GraphicsUnit.Point);
        using var smallFont = new Font(ReceiptFontName, ReceiptSmallSize, FontStyle.Regular, GraphicsUnit.Point);

        var doc = new PrintDocument();
        doc.PrinterSettings.PrinterName = printerName;
        doc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

        int lineIndex = 0;
        doc.PrintPage += (_, e) =>
        {
            float y = 0;
            var g = e.Graphics!;
            float lineHeight = baseFont.GetHeight(g);

            while (lineIndex < lines.Length)
            {
                // Strip ESC/POS bold sequences; detect bold/small lines
                var raw = lines[lineIndex];
                var isSmall = raw.Contains('\x0E');
                var clean = raw.Replace("\x0E", "").Replace("\x1B\x45\x01", "").Replace("\x1B\x45\x00", "");
                var font = isSmall ? smallFont : raw.Contains("\x1B\x45\x01") ? boldFont : baseFont;

                if (!string.IsNullOrEmpty(clean))
                    g.DrawString(clean, font, Brushes.Black, 0f, y);

                y += lineHeight;
                lineIndex++;

                if (lineIndex < lines.Length && y + lineHeight > e.PageBounds.Height)
                {
                    e.HasMorePages = true;
                    return;
                }
            }
            e.HasMorePages = false;
        };

        doc.Print();
    }

    private static void RawPrint(string printerName, byte[] data)
    {
        if (!OpenPrinter(printerName, out var hPrinter, IntPtr.Zero))
            throw new InvalidOperationException($"Impossibile aprire la stampante: {printerName}");
        try
        {
            var docInfo = new DOCINFOA { pDocName = "Scontrino", pDataType = "RAW" };
            if (!StartDocPrinter(hPrinter, 1, docInfo))
                throw new InvalidOperationException("Impossibile avviare il documento di stampa.");
            try
            {
                StartPagePrinter(hPrinter);
                var ptr = Marshal.AllocHGlobal(data.Length);
                try
                {
                    Marshal.Copy(data, 0, ptr, data.Length);
                    if (!WritePrinter(hPrinter, ptr, data.Length, out _))
                        throw new InvalidOperationException("Errore durante la scrittura sulla stampante.");
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }
                EndPagePrinter(hPrinter);
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDocName = string.Empty;
        [MarshalAs(UnmanagedType.LPStr)] public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string pDataType = "RAW";
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In][MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);
}
