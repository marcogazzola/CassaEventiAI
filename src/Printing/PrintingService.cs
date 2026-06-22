using CassaEventiAI.Models;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Text;

namespace CassaEventiAI.Services;

public class PrintingService(ReceiptService receiptService)
{
    // ESC/POS commands
    private static readonly byte[] EscInit = [0x1B, 0x40];                // ESC @ – inizializza
    private static readonly byte[] EscCodePageCp1252 = [0x1B, 0x74, 0x10]; // ESC t 16 – WPC1252 (€ = 0x80)
    private static readonly byte[] GsCut = [0x1D, 0x56, 0x42, 0x00];      // GS V B 0 – taglio completo

    public List<string> GetInstalledPrinters()
        => PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(x => x).ToList();

    public string BuildSalePreview(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
        => receiptService.BuildTextPreview(sale, filterDepts, allDepartments);

    public void PrintSale(Sale sale, List<Department>? filterDepts = null, List<Department>? allDepartments = null)
    {
        if (!App.CurrentSettings.PrinterEnabled)
            return;
        PrintText(BuildSalePreview(sale, filterDepts, allDepartments));
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
            string.Empty);
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

        // CP1252: € viene codificato come 0x80, supportato da WPC1252 (ESC t 16)
        var encoding = Encoding.GetEncoding(1252);
        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var textBytes = encoding.GetBytes(normalized.Replace("\n", "\r\n"));

        var raw = new List<byte>();
        raw.AddRange(EscInit);
        raw.AddRange(EscCodePageCp1252);
        raw.AddRange(textBytes);
        raw.AddRange([0x0A, 0x0A, 0x0A, 0x0A]); // avanzamento carta prima del taglio
        raw.AddRange(GsCut);

        RawPrint(printerName, [.. raw]);
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
