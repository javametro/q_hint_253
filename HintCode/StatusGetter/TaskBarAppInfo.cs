using StatusGetter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing taskbar pinned app information
    /// </summary>
    public interface ITaskBarAppInfo
    {
        /// <summary>
        /// Gets information about all applications pinned to the taskbar
        /// </summary>
        /// <returns>List of taskbar app information objects</returns>
        List<TaskBarAppItem> GetPinnedApps();

        /// <summary>
        /// Checks if a specific application is pinned to the taskbar
        /// </summary>
        /// <param name="appPath">Path to the application executable</param>
        /// <returns>True if the application is pinned, false otherwise</returns>
        bool IsAppPinned(string appPath);

        /// <summary>
        /// Gets the path to the folder containing taskbar pinned app shortcuts
        /// </summary>
        /// <returns>Path to the taskbar shortcuts folder</returns>
        string GetTaskBarPinFolder();

        /// <summary>
        /// Refreshes the pinned app information
        /// </summary>
        /// <returns>List of taskbar app information objects</returns>
        List<TaskBarAppItem> RefreshPinnedApps();
    }

    /// <summary>
    /// Contains information about an application pinned to the taskbar
    /// </summary>
    public class TaskBarAppItem
    {
        /// <summary>
        /// Name of the shortcut file (without extension)
        /// </summary>
        public string ShortcutName { get; set; }

        /// <summary>
        /// Full path to the shortcut file
        /// </summary>
        public string ShortcutPath { get; set; }

        /// <summary>
        /// Full path to the target application
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// Command line arguments for the target application
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Working directory for the target application
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Description of the shortcut
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Icon index in the target file
        /// </summary>
        public int IconIndex { get; set; }

        /// <summary>
        /// Name of the target file (without path)
        /// </summary>
        public string TargetFileName => Path.GetFileName(TargetPath);

        /// <summary>
        /// File extension of the target application
        /// </summary>
        public string TargetExtension => Path.GetExtension(TargetPath);

        /// <summary>
        /// Whether the target application exists
        /// </summary>
        public bool TargetExists => File.Exists(TargetPath);

        /// <summary>
        /// Returns a string representation of the pinned app
        /// </summary>
        public override string ToString()
        {
            return $"{ShortcutName} -> {TargetPath}";
        }
    }

    /// <summary>
    /// Provides information about applications pinned to the taskbar
    /// </summary>
    public class TaskBarAppInfo : ITaskBarAppInfo
    {
        #region Native Methods and Interfaces

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        internal interface IPersistFile
        {
            void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        }

        #endregion

        private List<TaskBarAppItem> _pinnedApps;

        /// <summary>
        /// Initializes a new instance of the TaskBarAppInfo class
        /// </summary>
        public TaskBarAppInfo()
        {
            _pinnedApps = new List<TaskBarAppItem>();
            RefreshPinnedApps();
        }

        /// <summary>
        /// Gets information about all applications pinned to the taskbar
        /// </summary>
        /// <returns>List of taskbar app information objects</returns>
        public List<TaskBarAppItem> GetPinnedApps()
        {
            return _pinnedApps;
        }

        /// <summary>
        /// Checks if a specific application is pinned to the taskbar
        /// </summary>
        /// <param name="appPath">Path to the application executable</param>
        /// <returns>True if the application is pinned, false otherwise</returns>
        public bool IsAppPinned(string appPath)
        {
            if (string.IsNullOrEmpty(appPath))
                return false;

            string normalizedPath = Path.GetFullPath(appPath).ToLowerInvariant();
            return _pinnedApps.Exists(app => app.TargetPath.ToLowerInvariant() == normalizedPath);
        }

        /// <summary>
        /// Gets the path to the folder containing taskbar pinned app shortcuts
        /// </summary>
        /// <returns>Path to the taskbar shortcuts folder</returns>
        public string GetTaskBarPinFolder()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string taskbarFolder = Path.Combine(appDataPath, @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

            return Directory.Exists(taskbarFolder) ? taskbarFolder : null;
        }

        /// <summary>
        /// Refreshes the pinned app information
        /// </summary>
        /// <returns>List of taskbar app information objects</returns>
        public List<TaskBarAppItem> RefreshPinnedApps()
        {
            _pinnedApps.Clear();

            try
            {
                string taskbarFolder = GetTaskBarPinFolder();
                if (string.IsNullOrEmpty(taskbarFolder) || !Directory.Exists(taskbarFolder))
                {
                    return _pinnedApps;
                }

                // Get all .lnk files in the taskbar folder
                string[] shortcutFiles = Directory.GetFiles(taskbarFolder, "*.lnk");
                foreach (string shortcutPath in shortcutFiles)
                {
                    try
                    {
                        TaskBarAppItem appItem = ParseShortcut(shortcutPath);
                        if (appItem != null)
                        {
                            _pinnedApps.Add(appItem);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing shortcut {shortcutPath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing pinned apps: {ex.Message}");
            }

            return _pinnedApps;
        }

        #region Helper Methods

        /// <summary>
        /// Parses a shortcut file to extract target information
        /// </summary>
        private TaskBarAppItem ParseShortcut(string shortcutPath)
        {
            if (!File.Exists(shortcutPath) || !shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            TaskBarAppItem appItem = new TaskBarAppItem
            {
                ShortcutPath = shortcutPath,
                ShortcutName = Path.GetFileNameWithoutExtension(shortcutPath)
            };

            try
            {
                ShellLink link = new ShellLink();
                IShellLink shellLink = (IShellLink)link;
                IPersistFile persistFile = (IPersistFile)link;

                // Load the shortcut
                persistFile.Load(shortcutPath, 0);

                // Get target path
                StringBuilder targetPath = new StringBuilder(260);
                shellLink.GetPath(targetPath, targetPath.Capacity, out IntPtr _, 0);
                appItem.TargetPath = targetPath.ToString();

                // Get arguments
                StringBuilder arguments = new StringBuilder(1024);
                shellLink.GetArguments(arguments, arguments.Capacity);
                appItem.Arguments = arguments.ToString();

                // Get working directory
                StringBuilder workingDir = new StringBuilder(260);
                shellLink.GetWorkingDirectory(workingDir, workingDir.Capacity);
                appItem.WorkingDirectory = workingDir.ToString();

                // Get description
                StringBuilder description = new StringBuilder(1024);
                shellLink.GetDescription(description, description.Capacity);
                appItem.Description = description.ToString();

                // Get icon location
                StringBuilder iconPath = new StringBuilder(260);
                shellLink.GetIconLocation(iconPath, iconPath.Capacity, out int iconIndex);
                appItem.IconIndex = iconIndex;

                // Marshal.ReleaseComObject(persistFile);
                // Marshal.ReleaseComObject(shellLink);
                // Marshal.ReleaseComObject(link);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing shortcut {shortcutPath}: {ex.Message}");
                return null;
            }

            return appItem;
        }

        #endregion
    }
}


//// Example: Get all apps pinned to the taskbar
//ITaskBarAppInfo taskBarInfo = new TaskBarAppInfo();
//List<TaskBarAppItem> pinnedApps = taskBarInfo.GetPinnedApps();

//Console.WriteLine($"Found {pinnedApps.Count} pinned applications:");
//foreach (var app in pinnedApps)
//{
//    Console.WriteLine($"Shortcut: {app.ShortcutName}");
//    Console.WriteLine($"  Target: {app.TargetPath}");
//    Console.WriteLine($"  Arguments: {app.Arguments}");
//    Console.WriteLine($"  Working Directory: {app.WorkingDirectory}");
//    Console.WriteLine($"  Target Exists: {app.TargetExists}");
//    Console.WriteLine();
//}

//// Example: Check if a specific app is pinned
//bool isEdgePinned = taskBarInfo.IsAppPinned(@"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe");
//Console.WriteLine($"Microsoft Edge is pinned: {isEdgePinned}");

//// Example: Get the taskbar pin folder
//string pinFolder = taskBarInfo.GetTaskBarPinFolder();
//Console.WriteLine($"Taskbar pin folder: {pinFolder}");



