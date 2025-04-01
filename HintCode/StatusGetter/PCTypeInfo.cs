using StatusGetter;
using System;
using System.Management;
using System.Runtime.InteropServices;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing PC type information
    /// </summary>
    public interface IPCTypeInfo
    {
        /// <summary>
        /// Gets the type of the PC
        /// </summary>
        /// <returns>PCType enum value indicating the PC type</returns>
        PCType GetPCType();

        /// <summary>
        /// Gets detailed information about the PC type
        /// </summary>
        /// <returns>PCTypeDetails object containing PC type information</returns>
        PCTypeDetails GetPCTypeDetails();

        /// <summary>
        /// Checks if the PC is a desktop system
        /// </summary>
        /// <returns>True if the system is a desktop, false otherwise</returns>
        bool IsDesktop();

        /// <summary>
        /// Checks if the PC is a notebook/laptop system
        /// </summary>
        /// <returns>True if the system is a notebook/laptop, false otherwise</returns>
        bool IsNotebook();

        /// <summary>
        /// Gets a user-friendly string description of the PC type
        /// </summary>
        /// <returns>String describing the PC type</returns>
        string GetPCTypeDescription();

        /// <summary>
        /// Gets detailed PC model information
        /// </summary>
        /// <returns>PCModelInfo object containing detailed model information</returns>
        PCModelInfo GetModelInfo();

        /// <summary>
        /// Gets the time when the PC was last operated using mouse or keyboard
        /// </summary>
        /// <returns>DateTime representing the last input time</returns>
        DateTime GetLastInputTime();

        /// <summary>
        /// Gets the time elapsed since the last user input (mouse or keyboard)
        /// </summary>
        /// <returns>TimeSpan representing the idle time</returns>
        TimeSpan GetIdleTime();
    }

    /// <summary>
    /// Enum representing PC types
    /// </summary>
    public enum PCType
    {
        /// <summary>
        /// Desktop computer
        /// </summary>
        Desktop = 1,

        /// <summary>
        /// Notebook/Laptop computer
        /// </summary>
        Notebook = 2,

        /// <summary>
        /// Workstation
        /// </summary>
        Workstation = 3,

        /// <summary>
        /// Enterprise Server
        /// </summary>
        EnterpriseServer = 4,

        /// <summary>
        /// Small Office and Home Office (SOHO) Server
        /// </summary>
        SOHOServer = 5,

        /// <summary>
        /// Appliance PC
        /// </summary>
        AppliancePC = 6,

        /// <summary>
        /// Performance Server
        /// </summary>
        PerformanceServer = 7,

        /// <summary>
        /// Maximum performance personal computer
        /// </summary>
        Maximum = 8,

        /// <summary>
        /// Tablet
        /// </summary>
        Tablet = 9,

        /// <summary>
        /// Convertible
        /// </summary>
        Convertible = 10,

        /// <summary>
        /// Detachable
        /// </summary>
        Detachable = 11,

        /// <summary>
        /// IoT Gateway
        /// </summary>
        IoTGateway = 12,

        /// <summary>
        /// Embedded PC
        /// </summary>
        EmbeddedPC = 13,

        /// <summary>
        /// Mini PC
        /// </summary>
        MiniPC = 14,

        /// <summary>
        /// Stick PC
        /// </summary>
        StickPC = 15,

        /// <summary>
        /// Unknown PC type
        /// </summary>
        Unknown = 0
    }

    /// <summary>
    /// Contains detailed information about the PC type
    /// </summary>
    public class PCTypeDetails
    {
        /// <summary>
        /// Type of PC
        /// </summary>
        public PCType PCType { get; set; }

        /// <summary>
        /// User-friendly description of the PC type
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Method used to determine the PC type (WMI, Battery, etc.)
        /// </summary>
        public string DetectionMethod { get; set; }

        /// <summary>
        /// Whether the system has a battery
        /// </summary>
        public bool HasBattery { get; set; }

        /// <summary>
        /// Chassis type (Desktop, Laptop, etc.)
        /// </summary>
        public string ChassisType { get; set; }

        /// <summary>
        /// Manufacturer of the PC
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Model of the PC
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Whether the system is running on battery power
        /// </summary>
        public bool IsRunningOnBattery { get; set; }

        /// <summary>
        /// Returns a string representation of the PC type information
        /// </summary>
        public override string ToString()
        {
            return $"{Description} ({PCType}) - {Manufacturer} {Model}";
        }
    }

    /// <summary>
    /// Provides information about the PC type
    /// </summary>
    public class PCTypeInfo : IPCTypeInfo
    {
        #region Native Methods and Structures

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

        [StructLayout(LayoutKind.Sequential)]
        struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte Reserved1;
            public int BatteryLifeTime;
            public int BatteryFullLifeTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        #endregion

        // Existing methods remain unchanged...

        /// <summary>
        /// Gets detailed PC model information
        /// </summary>
        /// <returns>PCModelInfo object containing detailed model information</returns>
        public PCModelInfo GetModelInfo()
        {
            PCModelInfo modelInfo = new PCModelInfo();

            try
            {
                // Get computer system information
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT Manufacturer, Model, Name, SystemFamily, SystemSKUNumber FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        if (queryObj["Manufacturer"] != null)
                            modelInfo.Manufacturer = queryObj["Manufacturer"].ToString().Trim();

                        if (queryObj["Model"] != null)
                            modelInfo.Model = queryObj["Model"].ToString().Trim();

                        if (queryObj["Name"] != null)
                            modelInfo.ProductName = queryObj["Name"].ToString().Trim();

                        if (queryObj["SystemFamily"] != null)
                            modelInfo.SystemFamily = queryObj["SystemFamily"].ToString().Trim();

                        if (queryObj["SystemSKUNumber"] != null)
                            modelInfo.SystemSKU = queryObj["SystemSKUNumber"].ToString().Trim();
                    }
                }

                // Get BIOS information
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT Manufacturer, Name, SerialNumber, Version, ReleaseDate FROM Win32_BIOS"))
                {
                    foreach (ManagementObject queryObj in searcher.Get())
                    {
                        if (queryObj["SerialNumber"] != null)
                            modelInfo.SerialNumber = queryObj["SerialNumber"].ToString().Trim();

                        if (queryObj["Version"] != null)
                            modelInfo.BIOSVersion = queryObj["Version"].ToString().Trim();

                        if (queryObj["Manufacturer"] != null)
                            modelInfo.BIOSManufacturer = queryObj["Manufacturer"].ToString().Trim();

                        if (queryObj["ReleaseDate"] != null)
                        {
                            try
                            {
                                // WMI BIOS date is in format: YYYYMMDD000000.000000+000
                                string biosDate = queryObj["ReleaseDate"].ToString();
                                if (biosDate.Length >= 8)
                                {
                                    string dateString = biosDate.Substring(0, 8);
                                    if (DateTime.TryParseExact(dateString, "yyyyMMdd",
                                        System.Globalization.CultureInfo.InvariantCulture,
                                        System.Globalization.DateTimeStyles.None, out DateTime date))
                                    {
                                        modelInfo.BIOSReleaseDate = date;
                                    }
                                }
                            }
                            catch
                            {
                                // If date parsing fails, leave as null
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting PC model information: {ex.Message}");
            }

            return modelInfo;
        }

        /// <summary>
        /// Gets the time when the PC was last operated using mouse or keyboard
        /// </summary>
        /// <returns>DateTime representing the last input time</returns>
        public DateTime GetLastInputTime()
        {
            try
            {
                LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

                if (GetLastInputInfo(ref lastInputInfo))
                {
                    // Get the system uptime in milliseconds
                    uint systemUptime = (uint)Environment.TickCount;

                    // Calculate the last input time in milliseconds
                    uint lastInputTicks = systemUptime - (lastInputInfo.dwTime);

                    // Convert to DateTime
                    DateTime lastInputTime = DateTime.Now.AddMilliseconds(-lastInputTicks);

                    return lastInputTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting last input time: {ex.Message}");
            }

            // If there was an error, return current time
            return DateTime.Now;
        }

        /// <summary>
        /// Gets the time elapsed since the last user input (mouse or keyboard)
        /// </summary>
        /// <returns>TimeSpan representing the idle time</returns>
        public TimeSpan GetIdleTime()
        {
            try
            {
                LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
                lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

                if (GetLastInputInfo(ref lastInputInfo))
                {
                    // Get the system uptime in milliseconds
                    uint systemUptime = (uint)Environment.TickCount;

                    // Calculate the idle time in milliseconds
                    uint idleTime = systemUptime - lastInputInfo.dwTime;

                    return TimeSpan.FromMilliseconds(idleTime);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting idle time: {ex.Message}");
            }

            return TimeSpan.Zero;
        }

        PCType IPCTypeInfo.GetPCType()
        {
            throw new NotImplementedException();
        }

        PCTypeDetails IPCTypeInfo.GetPCTypeDetails()
        {
            throw new NotImplementedException();
        }

        bool IPCTypeInfo.IsDesktop()
        {
            throw new NotImplementedException();
        }

        bool IPCTypeInfo.IsNotebook()
        {
            throw new NotImplementedException();
        }

        string IPCTypeInfo.GetPCTypeDescription()
        {
            throw new NotImplementedException();
        }

        PCModelInfo IPCTypeInfo.GetModelInfo()
        {
            throw new NotImplementedException();
        }

        DateTime IPCTypeInfo.GetLastInputTime()
        {
            throw new NotImplementedException();
        }

        TimeSpan IPCTypeInfo.GetIdleTime()
        {
            throw new NotImplementedException();
        }

        // Existing helper methods remain unchanged...
    }
    /// <summary>
    /// Contains detailed information about the PC model
    /// </summary>
    public class PCModelInfo
    {
        /// <summary>
        /// Manufacturer of the PC
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Model name or number of the PC
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Product name of the PC
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        /// Serial number of the PC
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// BIOS version
        /// </summary>
        public string BIOSVersion { get; set; }

        /// <summary>
        /// BIOS manufacturer
        /// </summary>
        public string BIOSManufacturer { get; set; }

        /// <summary>
        /// BIOS release date
        /// </summary>
        public DateTime? BIOSReleaseDate { get; set; }

        /// <summary>
        /// PC System Family
        /// </summary>
        public string SystemFamily { get; set; }

        /// <summary>
        /// PC System SKU
        /// </summary>
        public string SystemSKU { get; set; }

        /// <summary>
        /// Returns a string representation of the PC model information
        /// </summary>
        public override string ToString()
        {
            return $"{Manufacturer} {Model} - {ProductName}";
        }
    }
}


//// Example: Check the basic PC type
//IPCTypeInfo pcInfo = new PCTypeInfo();
//PCType type = pcInfo.GetPCType();
//Console.WriteLine($"PC Type: {type}");

//// Example: Check if this is a notebook
//bool isLaptop = pcInfo.IsNotebook();
//Console.WriteLine($"Is this a notebook/laptop? {isLaptop}");

//// Example: Get detailed PC type information
//PCTypeDetails details = pcInfo.GetPCTypeDetails();
//Console.WriteLine($"System: {details.Manufacturer} {details.Model}");
//Console.WriteLine($"Type: {details.Description}");
//Console.WriteLine($"Detection Method: {details.DetectionMethod}");
//Console.WriteLine($"Chassis Type: {details.ChassisType}");
//Console.WriteLine($"Has Battery: {details.HasBattery}");
//Console.WriteLine($"Running on Battery: {details.IsRunningOnBattery}");



//IPCTypeInfo pcInfo = new PCTypeInfo();

//// Get detailed model information
//PCModelInfo modelInfo = pcInfo.GetModelInfo();
//Console.WriteLine($"PC: {modelInfo.Manufacturer} {modelInfo.Model}");
//Console.WriteLine($"Product Name: {modelInfo.ProductName}");
//Console.WriteLine($"System Family: {modelInfo.SystemFamily}");
//Console.WriteLine($"Serial Number: {modelInfo.SerialNumber}");
//Console.WriteLine($"BIOS Version: {modelInfo.BIOSVersion}");
//if (modelInfo.BIOSReleaseDate.HasValue)
//    Console.WriteLine($"BIOS Date: {modelInfo.BIOSReleaseDate.Value.ToShortDateString()}");

//// Get last input time information
//DateTime lastInputTime = pcInfo.GetLastInputTime();
//TimeSpan idleTime = pcInfo.GetIdleTime();
//Console.WriteLine($"Last user activity: {lastInputTime}");
//Console.WriteLine($"System idle for: {idleTime.TotalMinutes:F2} minutes");
