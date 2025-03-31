using StatusGetter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Using the interface for dependency injection
            //IApplicationInstallStatus statusProvider = new ApplicationInstallStatus();
            //List<string> aumids = statusProvider.GetInstalledApplicationAumids();
            //// Print the AUMIDs
            //foreach (string aumid in aumids)
            //{
            //    // Print the AUMID more clearly
            //    Console.WriteLine($"AUMID: {aumid}");
            //}


            // Example: Get info for all drives
            IStorageInfo storageInfo = new StorageInfo();
            List<DriveStorageInfo> allDrives = storageInfo.GetAllDrivesInfo();

            foreach (var drive in allDrives)
            {
                Console.WriteLine(drive.ToString());
            }

            // Example: Get info for C drive
            DriveStorageInfo cDrive = storageInfo.GetDriveInfo("C");
            if (cDrive != null && cDrive.IsReady)
            {
                Console.WriteLine($"C drive - Total: {cDrive.TotalSizeFormatted}");
                Console.WriteLine($"Used: {cDrive.UsedSpaceFormatted} ({cDrive.UsedSpacePercent:0.#}%)");
                Console.WriteLine($"Free: {cDrive.FreeSpaceFormatted} ({cDrive.FreeSpacePercent:0.#}%)");
            }

        }
    }
}
