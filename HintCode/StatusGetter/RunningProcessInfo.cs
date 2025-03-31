using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusGetter
{
    //起動中のアプリケーション情報
    //起動中アプリの一覧情報を取得する。
    //（プロセスID、ウインドウハンドル、プロセス名、ウインドウタイトル、実行ファイルパス）
    //・バックグラウンドアプリなど、ウインドウを持たないアプリは対象外とする。
    //ManagementEventWatcherなどで、アプリ起動／終了をイベント取得し、常に起動中のアプリ一覧情報を把握するようにしたい。
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Text;

    namespace StatusGetter
    {
        /// <summary>
        /// Interface for accessing running process information
        /// </summary>
        public interface IRunningProcessInfo
        {
            /// <summary>
            /// Gets information about all running processes
            /// </summary>
            /// <returns>List of process information</returns>
            List<ProcessDetail> GetRunningProcesses();

            /// <summary>
            /// Gets information about a specific process by ID
            /// </summary>
            /// <param name="processId">The process ID to query</param>
            /// <returns>Process details or null if process not found</returns>
            ProcessDetail GetProcessById(int processId);

            /// <summary>
            /// Gets processes by name
            /// </summary>
            /// <param name="processName">The process name to search for</param>
            /// <returns>List of matching processes</returns>
            List<ProcessDetail> GetProcessesByName(string processName);
        }

        /// <summary>
        /// Data class to hold process details
        /// </summary>
        public class ProcessDetail
        {
            public int ProcessId { get; set; }
            public IntPtr WindowHandle { get; set; }
            public string ProcessName { get; set; }
            public string WindowTitle { get; set; }
            public string ExecutablePath { get; set; }

            public override string ToString()
            {
                return $"PID: {ProcessId}, Name: {ProcessName}, Title: {WindowTitle}, Path: {ExecutablePath}, Handle: {WindowHandle}";
            }
        }

        /// <summary>
        /// Provides information about running processes
        /// </summary>
        public class RunningProcessInfo : IRunningProcessInfo
        {
            // Windows API imports for getting window information
            [DllImport("user32.dll")]
            private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out int processId);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWindowVisible(IntPtr hWnd);

            private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

            /// <summary>
            /// Gets information about all running processes
            /// </summary>
            /// <returns>List of process information</returns>
            public List<ProcessDetail> GetRunningProcesses()
            {
                Dictionary<int, ProcessDetail> processDetails = new Dictionary<int, ProcessDetail>();

                // Get basic process information
                foreach (Process process in Process.GetProcesses())
                {
                    try
                    {
                        ProcessDetail detail = new ProcessDetail
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowHandle = process.MainWindowHandle,
                            WindowTitle = process.MainWindowTitle,
                            ExecutablePath = GetProcessPath(process)
                        };

                        processDetails[process.Id] = detail;
                    }
                    catch (Exception)
                    {
                        // Skip processes we don't have access to
                    }
                }

                // Enumerate windows to get more window handles
                EnumWindows((hWnd, lParam) =>
                {
                    if (!IsWindowVisible(hWnd))
                        return true;

                    int processId;
                    GetWindowThreadProcessId(hWnd, out processId);

                    if (processDetails.ContainsKey(processId))
                    {
                        var detail = processDetails[processId];

                        // If we don't have a window handle yet, or this is a different window
                        if (detail.WindowHandle == IntPtr.Zero || detail.WindowHandle != hWnd)
                        {
                            // If window title is empty, try to get a window title from this handle
                            if (string.IsNullOrEmpty(detail.WindowTitle))
                            {
                                int length = GetWindowTextLength(hWnd);
                                if (length > 0)
                                {
                                    StringBuilder sb = new StringBuilder(length + 1);
                                    GetWindowText(hWnd, sb, sb.Capacity);
                                    detail.WindowTitle = sb.ToString();
                                    detail.WindowHandle = hWnd;
                                }
                            }
                        }
                    }

                    return true;
                }, IntPtr.Zero);

                return new List<ProcessDetail>(processDetails.Values);
            }

            /// <summary>
            /// Gets information about a specific process by ID
            /// </summary>
            /// <param name="processId">The process ID to query</param>
            /// <returns>Process details or null if process not found</returns>
            public ProcessDetail GetProcessById(int processId)
            {
                try
                {
                    Process process = Process.GetProcessById(processId);
                    return new ProcessDetail
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        WindowHandle = process.MainWindowHandle,
                        WindowTitle = process.MainWindowTitle,
                        ExecutablePath = GetProcessPath(process)
                    };
                }
                catch (ArgumentException)
                {
                    // Process not found
                    return null;
                }
                catch (Exception)
                {
                    // Access denied or other error
                    return null;
                }
            }

            /// <summary>
            /// Gets processes by name
            /// </summary>
            /// <param name="processName">The process name to search for</param>
            /// <returns>List of matching processes</returns>
            public List<ProcessDetail> GetProcessesByName(string processName)
            {
                List<ProcessDetail> results = new List<ProcessDetail>();

                try
                {
                    foreach (Process process in Process.GetProcessesByName(processName))
                    {
                        results.Add(new ProcessDetail
                        {
                            ProcessId = process.Id,
                            ProcessName = process.ProcessName,
                            WindowHandle = process.MainWindowHandle,
                            WindowTitle = process.MainWindowTitle,
                            ExecutablePath = GetProcessPath(process)
                        });
                    }
                }
                catch (Exception)
                {
                    // Handle any access errors
                }

                return results;
            }

            /// <summary>
            /// Gets the executable path for a process
            /// </summary>
            private string GetProcessPath(Process process)
            {
                try
                {
                    return process.MainModule?.FileName;
                }
                catch (Exception)
                {
                    // For some system processes, we can't access this information
                    return string.Empty;
                }
            }
        }
    }
}
