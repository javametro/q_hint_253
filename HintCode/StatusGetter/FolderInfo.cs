﻿using StatusGetter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing folder information
    /// </summary>
    public interface IFolderInfo
    {
        /// <summary>
        /// Gets all file names in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <returns>List of file names</returns>
        List<string> GetFileNames(string folderPath);

        /// <summary>
        /// Gets all file names with specified extension in the folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <param name="extension">File extension to filter (e.g., ".txt")</param>
        /// <returns>List of file names with the specified extension</returns>
        List<string> GetFileNamesByExtension(string folderPath, string extension);

        /// <summary>
        /// Gets detailed information about all files in the folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <param name="searchPattern">Search pattern (e.g., "*.txt")</param>
        /// <param name="includeSubfolders">Whether to include files in subfolders</param>
        /// <returns>List of file information objects</returns>
        List<FileDetailInfo> GetFileDetails(string folderPath, string searchPattern = "*.*", bool includeSubfolders = false);

        /// <summary>
        /// Gets all subfolder names in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <returns>List of subfolder names</returns>
        List<string> GetSubfolderNames(string folderPath);
    }

    /// <summary>
    /// Data class to hold detailed file information
    /// </summary>
    public class FileDetailInfo
    {
        /// <summary>
        /// Full path to the file
        /// </summary>
        public string FullPath { get; set; }

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
        /// File last access time
        /// </summary>
        public DateTime LastAccessTime { get; set; }

        /// <summary>
        /// File last write time
        /// </summary>
        public DateTime LastWriteTime { get; set; }

        /// <summary>
        /// File attributes
        /// </summary>
        public FileAttributes Attributes { get; set; }

        /// <summary>
        /// Formatted file size (e.g., "1.5 MB")
        /// </summary>
        public string FormattedSize => FormatSize(SizeBytes);

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
        /// Returns a string representation of the file information
        /// </summary>
        public override string ToString()
        {
            return $"{FileName} ({FormattedSize}) - Modified: {LastWriteTime}";
        }
    }

    /// <summary>
    /// Provides information about folders and their contents
    /// </summary>
    public class FolderInfo : IFolderInfo
    {
        /// <summary>
        /// Gets all file names in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <returns>List of file names</returns>
        public List<string> GetFileNames(string folderPath)
        {
            List<string> fileNames = new List<string>();

            try
            {
                if (Directory.Exists(folderPath))
                {
                    string[] files = Directory.GetFiles(folderPath);
                    foreach (string file in files)
                    {
                        fileNames.Add(Path.GetFileName(file));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing folder {folderPath}: {ex.Message}");
            }

            return fileNames;
        }

        /// <summary>
        /// Gets all file names with specified extension in the folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <param name="extension">File extension to filter (e.g., ".txt")</param>
        /// <returns>List of file names with the specified extension</returns>
        public List<string> GetFileNamesByExtension(string folderPath, string extension)
        {
            List<string> fileNames = new List<string>();

            try
            {
                if (Directory.Exists(folderPath))
                {
                    // Ensure extension starts with a dot
                    if (!string.IsNullOrEmpty(extension) && !extension.StartsWith("."))
                    {
                        extension = "." + extension;
                    }

                    string[] files = Directory.GetFiles(folderPath, $"*{extension}");
                    foreach (string file in files)
                    {
                        fileNames.Add(Path.GetFileName(file));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing folder {folderPath}: {ex.Message}");
            }

            return fileNames;
        }

        /// <summary>
        /// Gets detailed information about all files in the folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <param name="searchPattern">Search pattern (e.g., "*.txt")</param>
        /// <param name="includeSubfolders">Whether to include files in subfolders</param>
        /// <returns>List of file information objects</returns>
        public List<FileDetailInfo> GetFileDetails(string folderPath, string searchPattern = "*.*", bool includeSubfolders = false)
        {
            List<FileDetailInfo> fileDetails = new List<FileDetailInfo>();

            try
            {
                if (Directory.Exists(folderPath))
                {
                    SearchOption searchOption = includeSubfolders
                        ? SearchOption.AllDirectories
                        : SearchOption.TopDirectoryOnly;

                    string[] files = Directory.GetFiles(folderPath, searchPattern, searchOption);

                    foreach (string filePath in files)
                    {
                        try
                        {
                            FileInfo fileInfo = new FileInfo(filePath);

                            fileDetails.Add(new FileDetailInfo
                            {
                                FullPath = fileInfo.FullName,
                                FileName = fileInfo.Name,
                                Extension = fileInfo.Extension,
                                SizeBytes = fileInfo.Length,
                                CreationTime = fileInfo.CreationTime,
                                LastAccessTime = fileInfo.LastAccessTime,
                                LastWriteTime = fileInfo.LastWriteTime,
                                Attributes = fileInfo.Attributes
                            });
                        }
                        catch (Exception)
                        {
                            // Skip files that can't be accessed
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing folder {folderPath}: {ex.Message}");
            }

            return fileDetails;
        }

        /// <summary>
        /// Gets all subfolder names in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder</param>
        /// <returns>List of subfolder names</returns>
        public List<string> GetSubfolderNames(string folderPath)
        {
            List<string> folderNames = new List<string>();

            try
            {
                if (Directory.Exists(folderPath))
                {
                    string[] folders = Directory.GetDirectories(folderPath);
                    foreach (string folder in folders)
                    {
                        folderNames.Add(Path.GetFileName(folder));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing folder {folderPath}: {ex.Message}");
            }

            return folderNames;
        }
    }
}


// Example: Get all file names in a folder
//IFolderInfo folderInfo = new FolderInfo();
//List<string> files = folderInfo.GetFileNames(@"C:\Documents");

// Example: Get all text files
//List<string> textFiles = folderInfo.GetFileNamesByExtension(@"C:\Documents", ".txt");

// Example: Get detailed information about all image files
//List<FileDetailInfo> imageFiles = folderInfo.GetFileDetails(@"C:\Pictures", "*.jpg", true);
//foreach (var file in imageFiles)
//{
//    Console.WriteLine($"{file.FileName} - {file.FormattedSize} - Last modified: {file.LastWriteTime}");
//}

