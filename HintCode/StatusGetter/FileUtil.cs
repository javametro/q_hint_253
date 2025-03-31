using StatusGetter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace StatusGetter
{
    /// <summary>
    /// Interface for file utility operations
    /// </summary>
    public interface IFileUtil
    {
        /// <summary>
        /// Checks if a file exists at the specified path
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Checks if a file exists and is accessible (can be read)
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>True if the file exists and is accessible, false otherwise</returns>
        bool FileExistsAndAccessible(string filePath);

        /// <summary>
        /// Gets information about a file if it exists
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>FileInfo object if the file exists, null otherwise</returns>
        FileInfo GetFileInfo(string filePath);

        /// <summary>
        /// Calculates the MD5 hash of a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>MD5 hash as a hexadecimal string, or null if the file doesn't exist</returns>
        string CalculateFileMD5(string filePath);

        /// <summary>
        /// Asynchronously checks if a file exists at the specified path
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>Task that resolves to true if the file exists, false otherwise</returns>
        Task<bool> FileExistsAsync(string filePath);
    }

    /// <summary>
    /// Utility class for file operations
    /// </summary>
    public class FileUtil : IFileUtil
    {
        /// <summary>
        /// Checks if a file exists at the specified path
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public bool FileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if file exists: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a file exists and is accessible (can be read)
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>True if the file exists and is accessible, false otherwise</returns>
        public bool FileExistsAndAccessible(string filePath)
        {
            if (!FileExists(filePath))
                return false;

            try
            {
                // Try to open the file to verify it's accessible
                using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Gets information about a file if it exists
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>FileInfo object if the file exists, null otherwise</returns>
        public FileInfo GetFileInfo(string filePath)
        {
            if (!FileExists(filePath))
                return null;

            try
            {
                return new FileInfo(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting file info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculates the MD5 hash of a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>MD5 hash as a hexadecimal string, or null if the file doesn't exist</returns>
        public string CalculateFileMD5(string filePath)
        {
            if (!FileExists(filePath))
                return null;

            try
            {
                using (var md5 = MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        byte[] hash = md5.ComputeHash(stream);
                        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating MD5 hash: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Asynchronously checks if a file exists at the specified path
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <returns>Task that resolves to true if the file exists, false otherwise</returns>
        public async Task<bool> FileExistsAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            try
            {
                return await Task.Run(() => File.Exists(filePath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if file exists asynchronously: {ex.Message}");
                return false;
            }
        }
    }
}


//// Example: Check if a file exists
//IFileUtil fileUtil = new FileUtil();
//bool exists = fileUtil.FileExists(@"C:\path\to\file.txt");
//Console.WriteLine($"File exists: {exists}");

//// Example: Get file information
//FileInfo fileInfo = fileUtil.GetFileInfo(@"C:\path\to\file.txt");
//if (fileInfo != null)
//{
//    Console.WriteLine($"File size: {fileInfo.Length} bytes");
//    Console.WriteLine($"Last modified: {fileInfo.LastWriteTime}");
//}

//// Example: Calculate file MD5 hash
//string hash = fileUtil.CalculateFileMD5(@"C:\path\to\file.txt");
//Console.WriteLine($"File MD5: {hash}");

//// Example: Async check
//bool existsAsync = await fileUtil.FileExistsAsync(@"C:\path\to\file.txt");
