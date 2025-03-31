using StatusGetter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing monitor information
    /// </summary>
    public interface IMonitorInfo
    {
        /// <summary>
        /// Gets information about all connected monitors
        /// </summary>
        /// <returns>List of monitor information</returns>
        List<MonitorDetails> GetAllMonitors();

        /// <summary>
        /// Gets information about the primary monitor
        /// </summary>
        /// <returns>Monitor details for the primary monitor</returns>
        MonitorDetails GetPrimaryMonitor();

        /// <summary>
        /// Gets the total desktop resolution across all monitors
        /// </summary>
        /// <returns>Size representing the total desktop area</returns>
        Size GetTotalDesktopResolution();

        /// <summary>
        /// Gets information about a specific monitor by device name
        /// </summary>
        /// <param name="deviceName">Device name of the monitor</param>
        /// <returns>Monitor details or null if not found</returns>
        MonitorDetails GetMonitorByDeviceName(string deviceName);
    }

    /// <summary>
    /// Contains detailed information about a monitor
    /// </summary>
    public class MonitorDetails
    {
        /// <summary>
        /// Device name of the monitor (e.g., "\\.\DISPLAY1")
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Friendly name of the monitor (e.g., "Generic PnP Monitor")
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Monitor bounds in screen coordinates
        /// </summary>
        public Rectangle Bounds { get; set; }

        /// <summary>
        /// Working area of the monitor (excluding taskbar)
        /// </summary>
        public Rectangle WorkingArea { get; set; }

        /// <summary>
        /// Whether this is the primary monitor
        /// </summary>
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Horizontal DPI of the monitor
        /// </summary>
        public int DpiX { get; set; }

        /// <summary>
        /// Vertical DPI of the monitor
        /// </summary>
        public int DpiY { get; set; }

        /// <summary>
        /// Physical width of the monitor in millimeters (if available)
        /// </summary>
        public int? PhysicalWidthMm { get; set; }

        /// <summary>
        /// Physical height of the monitor in millimeters (if available)
        /// </summary>
        public int? PhysicalHeightMm { get; set; }

        /// <summary>
        /// Bit depth of the monitor (color depth)
        /// </summary>
        public int BitDepth { get; set; }

        /// <summary>
        /// Refresh rate of the monitor in Hz
        /// </summary>
        public int RefreshRate { get; set; }

        /// <summary>
        /// Resolution of the monitor
        /// </summary>
        public Size Resolution => new Size(Bounds.Width, Bounds.Height);

        /// <summary>
        /// Diagonal size of the monitor in inches (if physical dimensions available)
        /// </summary>
        public double? DiagonalSizeInches
        {
            get
            {
                if (!PhysicalWidthMm.HasValue || !PhysicalHeightMm.HasValue ||
                    PhysicalWidthMm.Value <= 0 || PhysicalHeightMm.Value <= 0)
                    return null;

                double widthInches = PhysicalWidthMm.Value / 25.4;
                double heightInches = PhysicalHeightMm.Value / 25.4;
                return Math.Sqrt(widthInches * widthInches + heightInches * heightInches);
            }
        }

        /// <summary>
        /// Type of the monitor (Primary or Secondary)
        /// </summary>
        public string MonitorType => IsPrimary ? "Primary" : "Secondary";

        /// <summary>
        /// String representation of the monitor information
        /// </summary>
        public override string ToString()
        {
            return $"{DisplayName} ({DeviceName}): {Resolution.Width}x{Resolution.Height}, " +
                   $"{RefreshRate}Hz, {BitDepth}-bit, DPI: {DpiX}x{DpiY}, {MonitorType}";
        }
    }

    /// <summary>
    /// Provides information about connected monitors
    /// </summary>
    public class MonitorInfo : IMonitorInfo
    {
        #region Native Methods and Structures

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("shcore.dll")]
        private static extern IntPtr GetDpiForMonitor(IntPtr hmonitor, DpiType dpiType, out uint dpiX, out uint dpiY);

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        private enum DpiType
        {
            Effective = 0,
            Angular = 1,
            Raw = 2
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public int Width => right - left;
            public int Height => bottom - top;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DEVMODE
        {
            private const int CCHDEVICENAME = 32;
            private const int CCHFORMNAME = 32;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        // Constants
        private const int ENUM_CURRENT_SETTINGS = -1;
        private const int DISPLAY_DEVICE_ACTIVE = 0x1;
        private const int DISPLAY_DEVICE_PRIMARY_DEVICE = 0x4;

        #endregion

        /// <summary>
        /// Gets information about all connected monitors
        /// </summary>
        /// <returns>List of monitor information</returns>
        public List<MonitorDetails> GetAllMonitors()
        {
            List<MonitorDetails> monitors = new List<MonitorDetails>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX monitorInfo = new MONITORINFOEX();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
                GetMonitorInfo(hMonitor, ref monitorInfo);

                MonitorDetails monitor = new MonitorDetails
                {
                    DeviceName = monitorInfo.szDevice,
                    Bounds = new Rectangle(
                        monitorInfo.rcMonitor.left,
                        monitorInfo.rcMonitor.top,
                        monitorInfo.rcMonitor.Width,
                        monitorInfo.rcMonitor.Height),
                    WorkingArea = new Rectangle(
                        monitorInfo.rcWork.left,
                        monitorInfo.rcWork.top,
                        monitorInfo.rcWork.Width,
                        monitorInfo.rcWork.Height),
                    IsPrimary = (monitorInfo.dwFlags & DISPLAY_DEVICE_PRIMARY_DEVICE) != 0
                };

                // Get display name and additional details
                DISPLAY_DEVICE displayDevice = new DISPLAY_DEVICE();
                displayDevice.cb = Marshal.SizeOf(displayDevice);
                if (EnumDisplayDevices(monitorInfo.szDevice, 0, ref displayDevice, 0))
                {
                    monitor.DisplayName = displayDevice.DeviceString;
                }

                // Get DPI information
                GetDpiForMonitor(hMonitor, DpiType.Effective, out uint dpiX, out uint dpiY);
                monitor.DpiX = (int)dpiX;
                monitor.DpiY = (int)dpiY;

                // Get display settings (refresh rate, bit depth, etc.)
                DEVMODE devMode = new DEVMODE();
                devMode.dmSize = (short)Marshal.SizeOf(devMode);
                if (EnumDisplaySettings(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    monitor.RefreshRate = devMode.dmDisplayFrequency;
                    monitor.BitDepth = devMode.dmBitsPerPel;

                    // Calculate physical dimensions based on DPI (approximate)
                    if (devMode.dmPelsWidth > 0 && devMode.dmPelsHeight > 0 && dpiX > 0 && dpiY > 0)
                    {
                        monitor.PhysicalWidthMm = (int)(devMode.dmPelsWidth * 25.4 / dpiX);
                        monitor.PhysicalHeightMm = (int)(devMode.dmPelsHeight * 25.4 / dpiY);
                    }
                }

                monitors.Add(monitor);
                return true;
            }, IntPtr.Zero);

            return monitors;
        }

        /// <summary>
        /// Gets information about the primary monitor
        /// </summary>
        /// <returns>Monitor details for the primary monitor</returns>
        public MonitorDetails GetPrimaryMonitor()
        {
            List<MonitorDetails> monitors = GetAllMonitors();
            return monitors.Find(m => m.IsPrimary);
        }

        /// <summary>
        /// Gets the total desktop resolution across all monitors
        /// </summary>
        /// <returns>Size representing the total desktop area</returns>
        public Size GetTotalDesktopResolution()
        {
            return SystemInformation.VirtualScreen.Size;
        }

        /// <summary>
        /// Gets information about a specific monitor by device name
        /// </summary>
        /// <param name="deviceName">Device name of the monitor</param>
        /// <returns>Monitor details or null if not found</returns>
        public MonitorDetails GetMonitorByDeviceName(string deviceName)
        {
            List<MonitorDetails> monitors = GetAllMonitors();
            return monitors.Find(m => m.DeviceName.Equals(deviceName, StringComparison.OrdinalIgnoreCase));
        }
    }
}


//// Example: Get all connected monitors
//IMonitorInfo monitorInfo = new MonitorInfo();
//List<MonitorDetails> monitors = monitorInfo.GetAllMonitors();

//foreach (var monitor in monitors)
//{
//    Console.WriteLine(monitor.ToString());
//    Console.WriteLine($"Resolution: {monitor.Resolution.Width}x{monitor.Resolution.Height}");
//    Console.WriteLine($"Position: ({monitor.Bounds.X}, {monitor.Bounds.Y})");
//    Console.WriteLine($"DPI: {monitor.DpiX}x{monitor.DpiY}");
//    Console.WriteLine($"Refresh Rate: {monitor.RefreshRate} Hz");

//    if (monitor.DiagonalSizeInches.HasValue)
//    {
//        Console.WriteLine($"Diagonal Size: {monitor.DiagonalSizeInches:F1} inches");
//    }

//    Console.WriteLine($"Type: {monitor.MonitorType}");
//    Console.WriteLine();
//}

//// Example: Get primary monitor
//MonitorDetails primary = monitorInfo.GetPrimaryMonitor();
//Console.WriteLine($"Primary monitor: {primary.DisplayName}, {primary.Resolution.Width}x{primary.Resolution.Height}");

//// Example: Get total desktop area
//Size totalSize = monitorInfo.GetTotalDesktopResolution();
//Console.WriteLine($"Total desktop area: {totalSize.Width}x{totalSize.Height}");



