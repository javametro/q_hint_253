using StatusGetter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing files created before OOBE
    /// </summary>
    public interface IBeforeOOBEFileInfo
    {
        /// <summary>
        /// Gets the estimated OOBE date and time for the system
        /// </summary>
        /// <returns>Estimated OOBE date and time</returns>
        DateTime GetOOBEDateTime();

        /// <summary>
        /// Gets files created before OOBE in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of file information for files created before OOBE</returns>
        List<BeforeOOBEFile> GetFilesCreatedBeforeOOBE(string folderPath, bool includeSubfolders = true);

        /// <summary>
        /// Gets files created before OOBE in the specified folder with a specific extension
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="extension">File extension to filter (e.g., ".txt")</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of file information for files created before OOBE</returns>
        List<BeforeOOBEFile> GetFilesCreatedBeforeOOBEByExtension(string folderPath, string extension, bool includeSubfolders = true);

        /// <summary>
        /// Gets statistics about files created before OOBE
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>Statistics about files created before OOBE</returns>
        BeforeOOBEStatistics GetBeforeOOBEStatistics(string folderPath, bool includeSubfolders = true);
    }

    /// <summary>
    /// Information about a file created before OOBE
    /// </summary>
    public class BeforeOOBEFile
    {
        /// <summary>
        /// Full path to the file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File name with extension
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// File extension
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// File creation time
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// File last modification time
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// Difference between file creation time and OOBE time
        /// </summary>
        public TimeSpan TimeBeforeOOBE { get; set; }

        /// <summary>
        /// Formatted file size (e.g., "1.5 MB")
        /// </summary>
        public string FormattedSize => FormatSize(SizeBytes);

        /// <summary>
        /// Formats a size in bytes to a human-readable string
        /// </summary>
        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
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
        /// Returns a string representation of the file information
        /// </summary>
        public override string ToString()
        {
            return $"{FileName} ({FormattedSize}) - Created: {CreationTime} ({TimeBeforeOOBE.Days} days before OOBE)";
        }
    }

    /// <summary>
    /// Statistics about files created before OOBE
    /// </summary>
    public class BeforeOOBEStatistics
    {
        /// <summary>
        /// OOBE date and time
        /// </summary>
        public DateTime OOBEDateTime { get; set; }

        /// <summary>
        /// Total number of files created before OOBE
        /// </summary>
        public int FileCount { get; set; }

        /// <summary>
        /// Total size of files created before OOBE in bytes
        /// </summary>
        public long TotalSizeBytes { get; set; }

        /// <summary>
        /// Average size of files created before OOBE in bytes
        /// </summary>
        public long AverageSizeBytes => FileCount > 0 ? TotalSizeBytes / FileCount : 0;

        /// <summary>
        /// Number of files by extension
        /// </summary>
        public Dictionary<string, int> FileCountByExtension { get; set; }

        /// <summary>
        /// Earliest file creation time
        /// </summary>
        public DateTime? EarliestFileCreationTime { get; set; }

        /// <summary>
        /// Average time before OOBE that files were created
        /// </summary>
        public TimeSpan AverageTimeBeforeOOBE { get; set; }

        /// <summary>
        /// Formatted total size (e.g., "1.5 GB")
        /// </summary>
        public string FormattedTotalSize => FormatSize(TotalSizeBytes);

        /// <summary>
        /// Formatted average size (e.g., "250 KB")
        /// </summary>
        public string FormattedAverageSize => FormatSize(AverageSizeBytes);

        /// <summary>
        /// Formats a size in bytes to a human-readable string
        /// </summary>
        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }

    //指定フォルダのOOBE以前のファイル情報
    //指定フォルダ内（サブフォルダ含む）のOOBE以前に作成されたファイルの一覧情報を取得する。
    //OOBE時刻は、ドキュメントやピクチャのフォルダ作成日時を参照。
    public class BeforeOOBEFileInfo : IBeforeOOBEFileInfo
    {
        private DateTime? _oobeDateTime;

        /// <summary>
        /// Gets the estimated OOBE date and time for the system
        /// </summary>
        /// <returns>Estimated OOBE date and time</returns>
        public DateTime GetOOBEDateTime()
        {
            // If we've already calculated the OOBE time, return it
            if (_oobeDateTime.HasValue)
            {
                return _oobeDateTime.Value;
            }

            try
            {
                // Get the creation time of common folders created during OOBE
                List<DateTime> folderCreationTimes = new List<DateTime>();

                string[] specialFolders = new[]
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                    Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                };

                foreach (string folder in specialFolders)
                {
                    if (Directory.Exists(folder))
                    {
                        DirectoryInfo dirInfo = new DirectoryInfo(folder);
                        folderCreationTimes.Add(dirInfo.CreationTime);
                    }
                }

                // If we found any folder creation times, use the earliest one as the OOBE time
                if (folderCreationTimes.Count > 0)
                {
                    _oobeDateTime = folderCreationTimes.Min();
                    return _oobeDateTime.Value;
                }

                // If we can't determine the OOBE time, use the Windows directory's creation time
                string windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                if (Directory.Exists(windowsDir))
                {
                    DirectoryInfo winDirInfo = new DirectoryInfo(windowsDir);
                    _oobeDateTime = winDirInfo.CreationTime;
                    return _oobeDateTime.Value;
                }

                // Fall back to system installation date
                string systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
                if (Directory.Exists(systemDir))
                {
                    DirectoryInfo sysDirInfo = new DirectoryInfo(systemDir);
                    _oobeDateTime = sysDirInfo.CreationTime;
                    return _oobeDateTime.Value;
                }

                // If all else fails, use a reasonable default (current time minus 1 day)
                _oobeDateTime = DateTime.Now.AddDays(-1);
                return _oobeDateTime.Value;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error determining OOBE time: {ex.Message}");
                // Default fallback
                _oobeDateTime = DateTime.Now.AddDays(-1);
                return _oobeDateTime.Value;
            }
        }

        /// <summary>
        /// Gets files created before OOBE in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of file information for files created before OOBE</returns>
        public List<BeforeOOBEFile> GetFilesCreatedBeforeOOBE(string folderPath, bool includeSubfolders = true)
        {
            List<BeforeOOBEFile> beforeOOBEFiles = new List<BeforeOOBEFile>();
            DateTime oobeTime = GetOOBEDateTime();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return beforeOOBEFiles;
                }

                SearchOption searchOption = includeSubfolders
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                string[] files = Directory.GetFiles(folderPath, "*.*", searchOption);

                foreach (string filePath in files)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(filePath);

                        // Check if the file was created before OOBE
                        if (fileInfo.CreationTime < oobeTime)
                        {
                            beforeOOBEFiles.Add(new BeforeOOBEFile
                            {
                                FilePath = filePath,
                                FileName = fileInfo.Name,
                                Extension = fileInfo.Extension,
                                SizeBytes = fileInfo.Length,
                                CreationTime = fileInfo.CreationTime,
                                LastWriteTime = fileInfo.LastWriteTime,
                                TimeBeforeOOBE = oobeTime - fileInfo.CreationTime
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing folder {folderPath}: {ex.Message}");
            }

            return beforeOOBEFiles;
        }

        /// <summary>
        /// Gets files created before OOBE in the specified folder with a specific extension
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="extension">File extension to filter (e.g., ".txt")</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of file information for files created before OOBE</returns>
        public List<BeforeOOBEFile> GetFilesCreatedBeforeOOBEByExtension(string folderPath, string extension, bool includeSubfolders = true)
        {
            List<BeforeOOBEFile> allFiles = GetFilesCreatedBeforeOOBE(folderPath, includeSubfolders);

            // Ensure extension starts with a dot
            if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            // Filter by extension
            return allFiles.Where(f => f.Extension.Equals(extension, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Gets statistics about files created before OOBE
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>Statistics about files created before OOBE</returns>
        public BeforeOOBEStatistics GetBeforeOOBEStatistics(string folderPath, bool includeSubfolders = true)
        {
            List<BeforeOOBEFile> files = GetFilesCreatedBeforeOOBE(folderPath, includeSubfolders);
            DateTime oobeTime = GetOOBEDateTime();

            BeforeOOBEStatistics statistics = new BeforeOOBEStatistics
            {
                OOBEDateTime = oobeTime,
                FileCount = files.Count,
                TotalSizeBytes = files.Sum(f => f.SizeBytes),
                FileCountByExtension = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            };

            // Calculate earliest file time
            if (files.Count > 0)
            {
                statistics.EarliestFileCreationTime = files.Min(f => f.CreationTime);
            }

            // Calculate average time before OOBE
            if (files.Count > 0)
            {
                long totalTicks = files.Sum(f => f.TimeBeforeOOBE.Ticks);
                statistics.AverageTimeBeforeOOBE = new TimeSpan(totalTicks / files.Count);
            }

            // Count files by extension
            foreach (var file in files)
            {
                string ext = string.IsNullOrEmpty(file.Extension) ? "(no extension)" : file.Extension.ToLower();

                if (statistics.FileCountByExtension.ContainsKey(ext))
                {
                    statistics.FileCountByExtension[ext]++;
                }
                else
                {
                    statistics.FileCountByExtension[ext] = 1;
                }
            }

            return statistics;
        }
    }
}

//// Example: Get the estimated OOBE time
//IBeforeOOBEFileInfo fileInfo = new BeforeOOBEFileInfo();
//DateTime oobeTime = fileInfo.GetOOBEDateTime();
//Console.WriteLine($"System OOBE completed on: {oobeTime}");

//// Example: Get all files created before OOBE in a folder
//List<BeforeOOBEFile> files = fileInfo.GetFilesCreatedBeforeOOBE(@"C:\Users\Public");
//Console.WriteLine($"Found {files.Count} files created before OOBE");

//// Example: Get statistics about files created before OOBE
//BeforeOOBEStatistics stats = fileInfo.GetBeforeOOBEStatistics(@"C:\Windows");
//Console.WriteLine($"Total files: {stats.FileCount}, Total size: {stats.FormattedTotalSize}");
//Console.WriteLine($"Average time before OOBE: {stats.AverageTimeBeforeOOBE.Days} days {stats.AverageTimeBeforeOOBE.Hours} hours");

//// Show file count by extension
//foreach (var ext in stats.FileCountByExtension)
//{
//    Console.WriteLine($"{ext.Key}: {ext.Value} files");
//}


