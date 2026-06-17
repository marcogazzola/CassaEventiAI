using System.IO;

namespace CplCassaEventi.Services;

public class UsbService
{
    public List<DriveInfo> GetUsbDrives()
        => DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Removable && d.IsReady)
            .ToList();

    public DriveInfo? GetFirstUsb()
        => GetUsbDrives().FirstOrDefault();
}
