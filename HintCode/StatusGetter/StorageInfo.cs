using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing storage information
    /// </summary>
    public interface IStorageInfo
    {
        /// <summary>
        /// Gets information for all available drives
        /// </summary>
        /// <returns>List of drive information</returns>
        List<DriveStorageInfo> GetAllDrivesInfo();

        /// <summary>
        /// Gets information for a specific drive
        /// </summary>
        /// <param name="driveName">Drive name (e.g., "C", "D")</param>
        /// <returns>Drive information or null if drive not found</returns>
        DriveStorageInfo GetDriveInfo(string driveName);
    }

    /// <summary>
    /// Data class to hold drive storage details
    /// </summary>
    public class DriveStorageInfo
    {
        /// <summary>
        /// Drive name (e.g., "C:", "D:")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Total size of the drive in bytes
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Used space on the drive in bytes
        /// </summary>
        public long UsedSpaceBytes { get; set; }

        /// <summary>
        /// Free space on the drive in bytes
        /// </summary>
        public long FreeSpaceBytes { get; set; }

        /// <summary>
        /// Percentage of the drive that is used (0-100)
        /// </summary>
        public double UsedSpacePercent { get; set; }

        /// <summary>
        /// Percentage of the drive that is free (0-100)
        /// </summary>
        public double FreeSpacePercent { get; set; }

        /// <summary>
        /// Drive format (e.g., NTFS, FAT32)
        /// </summary>
        public string DriveFormat { get; set; }

        /// <summary>
        /// Drive type (e.g., Fixed, Removable, Network)
        /// </summary>
        public DriveType DriveType { get; set; }

        /// <summary>
        /// Indicates if the drive is ready
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Total size of the drive as a formatted string (e.g., "500 GB")
        /// </summary>
        public string TotalSizeFormatted => FormatSize(TotalSizeBytes);

        /// <summary>
        /// Used space on the drive as a formatted string (e.g., "250 GB")
        /// </summary>
        public string UsedSpaceFormatted => FormatSize(UsedSpaceBytes);

        /// <summary>
        /// Free space on the drive as a formatted string (e.g., "250 GB")
        /// </summary>
        public string FreeSpaceFormatted => FormatSize(FreeSpaceBytes);

        /// <summary>
        /// Formats a size in bytes to a human-readable string
        /// </summary>
        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Returns a string representation of the drive information
        /// </summary>
        public override string ToString()
        {
            return $"{Name} - Total: {TotalSizeFormatted}, Used: {UsedSpaceFormatted} ({UsedSpacePercent:0.#}%), Free: {FreeSpaceFormatted} ({FreeSpacePercent:0.#}%)";
        }
    }

    //ストレージ情報
    //ドライブ毎（C、D）の全体容量、使用量（実数／％）、空き容量（実数／％）を取得する。
    //DriveInfoなどを使用する。
    public class StorageInfo : IStorageInfo
    {
        /// <summary>
        /// Gets information for all available drives
        /// </summary>
        /// <returns>List of drive information</returns>
        public List<DriveStorageInfo> GetAllDrivesInfo()
        {
            List<DriveStorageInfo> driveInfos = new List<DriveStorageInfo>();

            try
            {
                DriveInfo[] allDrives = DriveInfo.GetDrives();

                foreach (DriveInfo drive in allDrives)
                {
                    try
                    {
                        // Skip drives that aren't ready (like empty DVD drives)
                        if (!drive.IsReady)
                        {
                            driveInfos.Add(new DriveStorageInfo
                            {
                                Name = drive.Name,
                                IsReady = false,
                                DriveType = drive.DriveType
                            });
                            continue;
                        }

                        long totalSize = drive.TotalSize;
                        long freeSpace = drive.AvailableFreeSpace;
                        long usedSpace = totalSize - freeSpace;

                        double usedPercent = (double)usedSpace / totalSize * 100;
                        double freePercent = (double)freeSpace / totalSize * 100;

                        driveInfos.Add(new DriveStorageInfo
                        {
                            Name = drive.Name,
                            TotalSizeBytes = totalSize,
                            UsedSpaceBytes = usedSpace,
                            FreeSpaceBytes = freeSpace,
                            UsedSpacePercent = usedPercent,
                            FreeSpacePercent = freePercent,
                            DriveFormat = drive.DriveFormat,
                            DriveType = drive.DriveType,
                            IsReady = true
                        });
                    }
                    catch (Exception)
                    {
                        // Skip drives that throw exceptions
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting drive information: {ex.Message}");
            }

            return driveInfos;
        }

        /// <summary>
        /// Gets information for a specific drive
        /// </summary>
        /// <param name="driveName">Drive name (e.g., "C", "D")</param>
        /// <returns>Drive information or null if drive not found</returns>
        public DriveStorageInfo GetDriveInfo(string driveName)
        {
            try
            {
                // Normalize the drive name
                if (!driveName.EndsWith(":"))
                {
                    driveName = driveName + ":";
                }

                // Try to get the specified drive
                DriveInfo[] allDrives = DriveInfo.GetDrives();
                DriveInfo drive = allDrives.FirstOrDefault(d =>
                    d.Name.StartsWith(driveName, StringComparison.OrdinalIgnoreCase));

                if (drive == null)
                {
                    return null;
                }

                // If drive is not ready, return basic info
                if (!drive.IsReady)
                {
                    return new DriveStorageInfo
                    {
                        Name = drive.Name,
                        IsReady = false,
                        DriveType = drive.DriveType
                    };
                }

                // Calculate drive statistics
                long totalSize = drive.TotalSize;
                long freeSpace = drive.AvailableFreeSpace;
                long usedSpace = totalSize - freeSpace;

                double usedPercent = (double)usedSpace / totalSize * 100;
                double freePercent = (double)freeSpace / totalSize * 100;

                return new DriveStorageInfo
                {
                    Name = drive.Name,
                    TotalSizeBytes = totalSize,
                    UsedSpaceBytes = usedSpace,
                    FreeSpaceBytes = freeSpace,
                    UsedSpacePercent = usedPercent,
                    FreeSpacePercent = freePercent,
                    DriveFormat = drive.DriveFormat,
                    DriveType = drive.DriveType,
                    IsReady = true
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting information for drive {driveName}: {ex.Message}");
                return null;
            }
        }
    }
}
