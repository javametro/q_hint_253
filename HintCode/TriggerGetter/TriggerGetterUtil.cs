using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management; // Requires reference to System.Management.dll
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32; // Required for RegistryHive

namespace TriggerGetter
{
    #region Event Arguments Definition

    /// <summary>
    /// Provides data for application-related events (Start, Stop, Activate).
    /// </summary>
    public class ApplicationEventArgs : EventArgs
    {
        public int ProcessId { get; }
        public IntPtr WindowHandle { get; }
        public string ProcessName { get; }
        public string WindowTitle { get; }
        public string ExecutablePath { get; }

        public ApplicationEventArgs(int processId, IntPtr windowHandle, string processName, string windowTitle, string executablePath)
        {
            ProcessId = processId;
            WindowHandle = windowHandle; // Can be IntPtr.Zero if not available/applicable
            ProcessName = processName ?? string.Empty;
            WindowTitle = windowTitle ?? string.Empty; // Can be empty
            ExecutablePath = executablePath ?? string.Empty; // Can be empty if access denied
        }

        public override string ToString()
        {
            return $"PID={ProcessId}, Name='{ProcessName}', Path='{ExecutablePath}', HWND={WindowHandle}, Title='{WindowTitle}'";
        }
    }

    /// <summary>
    /// Provides data for window move or resize events, including location and size.
    /// Inherits application details from ApplicationEventArgs.
    /// </summary>
    public class WindowLocationEventArgs : ApplicationEventArgs
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public WindowLocationEventArgs(int processId, IntPtr windowHandle, string processName, string windowTitle, string executablePath, int x, int y, int width, int height)
            : base(processId, windowHandle, processName, windowTitle, executablePath)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
        public override string ToString()
        {
            return $"{base.ToString()}, Rect=({X},{Y} - {Width}x{Height})";
        }
    }

    /// <summary>
    /// Provides data for file system change events.
    /// </summary>
    public class FileSystemActivityEventArgs : EventArgs // Renamed from FileSystemEventArgs to avoid conflict with System.IO
    {
        public WatcherChangeTypes ChangeType { get; }
        public string FullPath { get; }
        public string Name { get; }
        public string OldFullPath { get; } // Only used for Rename events
        public string OldName { get; }     // Only used for Rename events

        public FileSystemActivityEventArgs(WatcherChangeTypes changeType, string fullPath, string name, string oldFullPath = null, string oldName = null)
        {
            ChangeType = changeType;
            FullPath = fullPath;
            Name = name;
            OldFullPath = oldFullPath;
            OldName = oldName;
        }
        public override string ToString()
        {
            return $"Type={ChangeType}, Path='{FullPath}'" +
                  (ChangeType == WatcherChangeTypes.Renamed ? $" (OldPath='{OldFullPath}')" : "");
        }
    }

    /// <summary>
    /// Provides data for registry key change events.
    /// Note: Only indicates *that* a change occurred, not the specific value.
    /// </summary>
    public class RegistryKeyChangedEventArgs : EventArgs
    {
        public RegistryHive Hive { get; }
        public string KeyPath { get; }

        public RegistryKeyChangedEventArgs(RegistryHive hive, string keyPath)
        {
            Hive = hive;
            KeyPath = keyPath;
        }
        public override string ToString()
        {
            return $"Hive={Hive}, KeyPath='{KeyPath}'";
        }
    }

    #endregion

    /// <summary>
    /// Utility class to monitor various system events like application lifecycle,
    /// window activation/movement, file system changes, and registry key modifications.
    /// Implements IDisposable to ensure proper cleanup of watchers, hooks, and handles.
    /// </summary>
    public class TriggerGetterUtil : IDisposable
    {
        #region Public Enums (Moved for Accessibility)

        /// <summary>
        /// Flags specifying the types of registry changes to monitor.
        /// Moved outside NativeMethods to be publicly accessible for StartMonitoringRegistryKey method signature.
        /// </summary>
        [Flags]
        public enum RegNotifyFilter : uint
        {
            REG_NOTIFY_CHANGE_NAME = 0x00000001, // Key added/deleted/renamed
            REG_NOTIFY_CHANGE_ATTRIBUTES = 0x00000002, // Key attribute changes
            REG_NOTIFY_CHANGE_LAST_SET = 0x00000004, // Value added/deleted/modified
            REG_NOTIFY_CHANGE_SECURITY = 0x00000008, // Security descriptor changes
        }

        #endregion

        #region Native Methods and Constants (P/Invoke Wrapper)

        /// <summary>
        /// Internal static class containing P/Invoke declarations for Windows API calls.
        /// </summary>
        private static class NativeMethods
        {
            // User32.dll functions for window events and information
            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            // Kernel32.dll functions for process handling
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, uint processId);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            // Psapi.dll for getting process module information (more reliable path)
            [DllImport("psapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] StringBuilder lpBaseName, [In][MarshalAs(UnmanagedType.U4)] int nSize);

            // Advapi32.dll functions for registry operations
            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern int RegOpenKeyEx(IntPtr hKey, string subKey, int ulOptions, int samDesired, out IntPtr hkResult);

            // *** Reference to RegNotifyFilter below now uses the public enum ***
            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, RegNotifyFilter dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

            [DllImport("advapi32.dll", SetLastError = true)]
            public static extern int RegCloseKey(IntPtr hKey);

            // Delegate signature for SetWinEventHook callback
            public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            // Structure for GetWindowRect
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

            // Registry Access Rights Constants
            public const int KEY_QUERY_VALUE = 0x0001;
            public const int KEY_NOTIFY = 0x0010;
            public const int STANDARD_RIGHTS_READ = 0x00020000;

            // Predefined Registry Hive Handles (from winnt.h)
            public static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));
            public static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));
            public static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));
            public static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));
            public static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004)); // Less common for monitoring
            public static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));
            public static readonly IntPtr HKEY_DYN_DATA = new IntPtr(unchecked((int)0x80000006)); // Deprecated

            // WinEvent Hook Constants (from winuser.h)
            public const uint WINEVENT_OUTOFCONTEXT = 0x0000; // Events are handled out of process context
            public const uint EVENT_SYSTEM_FOREGROUND = 0x0003; // Foreground window changed (activation)
            public const uint EVENT_SYSTEM_MOVESIZEEND = 0x000B; // Window finished moving/resizing
            //public const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B; // More granular, potentially noisy, fires for non-window objects too

            // Process Access Rights Flags (subset for getting path)
            [Flags]
            public enum ProcessAccessFlags : uint
            {
                QueryLimitedInformation = 0x1000
            }

            // *** RegNotifyFilter enum moved outside this class ***
        }

        #endregion

        #region Public Events

        // 1. Application Start/Stop Events (from WMI)
        /// <summary>
        /// Fired when a new process is detected starting.
        /// </summary>
        public event EventHandler<ApplicationEventArgs> ApplicationStarted;
        /// <summary>
        /// Fired when a process is detected stopping.
        /// </summary>
        public event EventHandler<ApplicationEventArgs> ApplicationStopped;

        // 2. Application Activation Event (from WinEventHook)
        /// <summary>
        /// Fired when the foreground window changes, indicating an application activation.
        /// </summary>
        public event EventHandler<ApplicationEventArgs> ApplicationActivated;

        // 3. Window Move/Size Change Event (from WinEventHook)
        /// <summary>
        /// Fired when a window finishes moving or resizing.
        /// </summary>
        public event EventHandler<WindowLocationEventArgs> WindowMovedOrResized;

        // 4. File System Change Event (from FileSystemWatcher)
        /// <summary>
        /// Fired when a file or directory is created, deleted, changed, or renamed in the monitored folder.
        /// </summary>
        public event EventHandler<FileSystemActivityEventArgs> FileSystemChanged;

        // 5. Registry Key Value Change Event (from RegNotifyChangeKeyValue)
        /// <summary>
        /// Fired when a change is detected under the monitored registry key (e.g., value set, subkey added/deleted).
        /// Note: This event only signals that *a* change occurred; it doesn't specify *what* changed.
        /// </summary>
        public event EventHandler<RegistryKeyChangedEventArgs> RegistryValueChanged;

        #endregion

        #region Private Fields

        // WMI Process Watchers for Start/Stop events
        private ManagementEventWatcher _processStartWatcher;
        private ManagementEventWatcher _processStopWatcher;

        // WinEvent Hooks for Foreground and Move/Size events
        private NativeMethods.WinEventDelegate _winEventDelegateInstance; // Crucial: Keep delegate instance alive for P/Invoke callback
        private IntPtr _hWinEventHookForeground = IntPtr.Zero;
        private IntPtr _hWinEventHookMoveSize = IntPtr.Zero;

        // File System Watcher for directory monitoring
        private FileSystemWatcher _fileSystemWatcher;

        // Registry Watcher components
        private Thread _registryWatcherThread;      // Dedicated thread for blocking RegNotifyChangeKeyValue call
        private ManualResetEvent _registryStopEvent; // Signal to stop the registry monitoring thread
        private IntPtr _registryHiveHandle = IntPtr.Zero; // Handle to the root hive (e.g., HKEY_CURRENT_USER)
        private IntPtr _registryKeyHandle = IntPtr.Zero;  // Handle to the specific subkey being monitored
        private RegistryHive _monitoredHive;         // Enum representation of the hive
        private string _monitoredRegistrySubKey;     // Path of the subkey under the hive
        private bool _watchSubtreeRegistry = false; // Flag to monitor subkeys recursively
        private RegNotifyFilter _registryNotifyFilter = RegNotifyFilter.REG_NOTIFY_CHANGE_LAST_SET; // Default filter (using public enum now)

        // Flag for IDisposable pattern
        private bool _disposed = false;

        #endregion

        #region Constructor and Destructor / IDisposable Implementation

        /// <summary>
        /// Initializes a new instance of the TriggerGetterUtil class.
        /// Sets up the delegate instance required for WinEventHooks.
        /// </summary>
        public TriggerGetterUtil()
        {
            // IMPORTANT: Store the delegate instance in a field to prevent it from being garbage collected
            // while the native code (SetWinEventHook) still holds a pointer to it.
            _winEventDelegateInstance = new NativeMethods.WinEventDelegate(WinEventProcCallback);
        }

        /// <summary>
        /// Finalizer (Destructor). Should not be relied upon; use Dispose explicitly.
        /// Calls Dispose with disposing=false to clean up unmanaged resources.
        /// </summary>
        ~TriggerGetterUtil()
        {
            Dispose(false);
        }

        /// <summary>
        /// Public implementation of IDisposable pattern.
        /// Cleans up both managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            // Prevent the finalizer from running since cleanup is already done.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected virtual implementation of the Dispose pattern.
        /// </summary>
        /// <param name="disposing">True if called from Dispose(), False if called from the finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return; // Avoid double disposal

            if (disposing)
            {
                // --- Dispose managed resources ---
                // Stop all monitoring activities first to release associated resources
                StopMonitoringApplicationEvents();
                StopMonitoringWindowEvents();
                StopMonitoringFileSystem();
                StopMonitoringRegistryKey(); // This signals the thread and closes handles

                // Dispose managed objects owned by this instance
                _registryStopEvent?.Dispose(); // Dispose the ManualResetEvent
                _fileSystemWatcher?.Dispose(); // Dispose the FileSystemWatcher
                _processStartWatcher?.Dispose(); // Dispose WMI watchers
                _processStopWatcher?.Dispose();
            }

            // --- Clean up unmanaged resources ---
            // Note: Unmanaged resources (hooks, handles) should ideally be cleaned up
            // within their respective StopMonitoring methods, which are called above.
            // This section is more of a safeguard or for finalizer path cleanup if needed,
            // but StopMonitoring should handle it. StopMonitoringRegistryKey handles _registryKeyHandle.
            // StopMonitoringWindowEvents handles the hooks.

            _disposed = true;
            Debug.WriteLine("TriggerGetterUtil Disposed.");
        }

        #endregion

        #region Public Start/Stop Monitoring Methods

        // --- 1. Application Start/Stop Monitoring ---

        /// <summary>
        /// Starts monitoring for process creation and termination using WMI.
        /// Requires appropriate WMI permissions.
        /// </summary>
        public void StartMonitoringApplicationEvents()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TriggerGetterUtil));
            if (_processStartWatcher != null || _processStopWatcher != null)
            {
                Debug.WriteLine("Application monitoring already started. Stopping first.");
                StopMonitoringApplicationEvents(); // Stop if already running to avoid duplicates
            }

            Debug.WriteLine("Starting Application Start/Stop Monitoring (WMI)...");
            try
            {
                // Monitor Process Start events
                // Note: Win32_ProcessStartTrace requires specific security policies/permissions.
                // Consider Win32_ProcessCreationEvent if StartTrace fails, but it might be less performant.
                WqlEventQuery startQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace");
                _processStartWatcher = new ManagementEventWatcher(startQuery);
                _processStartWatcher.EventArrived += ProcessStartedWmiHandler;
                _processStartWatcher.Start(); // Start listening for events
                Debug.WriteLine(" -> Process Start Watcher Started.");

                // Monitor Process Stop events
                WqlEventQuery stopQuery = new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace");
                _processStopWatcher = new ManagementEventWatcher(stopQuery);
                _processStopWatcher.EventArrived += ProcessStoppedWmiHandler;
                _processStopWatcher.Start(); // Start listening for events
                Debug.WriteLine(" -> Process Stop Watcher Started.");
            }
            catch (ManagementException mex)
            {
                // Common errors: WMI service not running, insufficient permissions, query issues.
                Debug.WriteLine($"ERROR starting WMI process monitoring: ManagementException - {mex.Message} (ErrorCode: {mex.ErrorCode})");
                StopMonitoringApplicationEvents(); // Clean up any partially started watchers
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR starting WMI process monitoring: Unexpected Exception - {ex.Message}");
                StopMonitoringApplicationEvents(); // Clean up
            }
        }

        /// <summary>
        /// Stops monitoring for process creation and termination.
        /// </summary>
        public void StopMonitoringApplicationEvents()
        {
            Debug.WriteLine("Stopping Application Start/Stop Monitoring (WMI)...");
            if (_processStartWatcher != null)
            {
                try { _processStartWatcher.Stop(); } catch (Exception ex) { Debug.WriteLine($"Error stopping start watcher: {ex.Message}"); }
                _processStartWatcher.EventArrived -= ProcessStartedWmiHandler; // Unsubscribe
                _processStartWatcher.Dispose(); // Release resources
                _processStartWatcher = null;
                Debug.WriteLine(" -> Process Start Watcher Stopped and Disposed.");
            }
            if (_processStopWatcher != null)
            {
                try { _processStopWatcher.Stop(); } catch (Exception ex) { Debug.WriteLine($"Error stopping stop watcher: {ex.Message}"); }
                _processStopWatcher.EventArrived -= ProcessStoppedWmiHandler; // Unsubscribe
                _processStopWatcher.Dispose(); // Release resources
                _processStopWatcher = null;
                Debug.WriteLine(" -> Process Stop Watcher Stopped and Disposed.");
            }
        }

        // --- 2 & 3. Window Activate & Move/Size Monitoring ---

        /// <summary>
        /// Starts monitoring for foreground window changes (activation) and window move/size completion events
        /// using Windows Event Hooks (SetWinEventHook).
        /// </summary>
        public void StartMonitoringWindowEvents()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TriggerGetterUtil));
            // Ensure previous hooks are removed before setting new ones
            StopMonitoringWindowEvents();

            Debug.WriteLine("Starting Window Event Monitoring (WinEventHook)...");

            // Hook for foreground window changes (Activation)
            // Monitors EVENT_SYSTEM_FOREGROUND event across all processes and threads.
            _hWinEventHookForeground = NativeMethods.SetWinEventHook(
                NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero, // hmodWinEventProc - NULL for out-of-context hooks
                _winEventDelegateInstance, // Pointer to the callback delegate instance
                0, // idProcess - 0 to monitor all processes
                0, // idThread - 0 to monitor all threads
                NativeMethods.WINEVENT_OUTOFCONTEXT); // Flag for out-of-context processing

            if (_hWinEventHookForeground == IntPtr.Zero)
            {
                // Hook failed - get error details
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"ERROR setting foreground hook: Failed with Win32 error code {errorCode}");
            }
            else
            {
                Debug.WriteLine($" -> Foreground Hook Set (Handle: {_hWinEventHookForeground}).");
            }


            // Hook for window move/size end events
            // Monitors EVENT_SYSTEM_MOVESIZEEND event across all processes and threads.
            _hWinEventHookMoveSize = NativeMethods.SetWinEventHook(
               NativeMethods.EVENT_SYSTEM_MOVESIZEEND, NativeMethods.EVENT_SYSTEM_MOVESIZEEND,
               IntPtr.Zero, _winEventDelegateInstance, 0, 0, NativeMethods.WINEVENT_OUTOFCONTEXT);

            if (_hWinEventHookMoveSize == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Debug.WriteLine($"ERROR setting move/size hook: Failed with Win32 error code {errorCode}");
                // If foreground hook succeeded but this failed, unhook the foreground one for consistency? Optional.
                // if (_hWinEventHookForeground != IntPtr.Zero) StopMonitoringWindowEvents();
            }
            else
            {
                Debug.WriteLine($" -> Move/Size Hook Set (Handle: {_hWinEventHookMoveSize}).");
            }
        }

        /// <summary>
        /// Stops monitoring for window events by unhooking the Windows Event Hooks.
        /// </summary>
        public void StopMonitoringWindowEvents()
        {
            Debug.WriteLine("Stopping Window Event Monitoring (WinEventHook)...");
            if (_hWinEventHookForeground != IntPtr.Zero)
            {
                if (NativeMethods.UnhookWinEvent(_hWinEventHookForeground))
                {
                    Debug.WriteLine($" -> Foreground Hook Unhooked (Handle: {_hWinEventHookForeground}).");
                }
                else
                {
                    Debug.WriteLine($" -> WARNING: Failed to unhook Foreground Hook (Handle: {_hWinEventHookForeground}). Error: {Marshal.GetLastWin32Error()}");
                }
                _hWinEventHookForeground = IntPtr.Zero; // Mark as unhooked regardless of success/failure
            }
            if (_hWinEventHookMoveSize != IntPtr.Zero)
            {
                if (NativeMethods.UnhookWinEvent(_hWinEventHookMoveSize))
                {
                    Debug.WriteLine($" -> Move/Size Hook Unhooked (Handle: {_hWinEventHookMoveSize}).");
                }
                else
                {
                    Debug.WriteLine($" -> WARNING: Failed to unhook Move/Size Hook (Handle: {_hWinEventHookMoveSize}). Error: {Marshal.GetLastWin32Error()}");
                }
                _hWinEventHookMoveSize = IntPtr.Zero; // Mark as unhooked
            }
        }

        // --- 4. File System Monitoring ---

        /// <summary>
        /// Starts monitoring a specified folder path for file system changes.
        /// </summary>
        /// <param name="folderPath">The full path to the directory to monitor.</param>
        /// <param name="includeSubdirectories">True to monitor subdirectories; otherwise, false.</param>
        /// <param name="notifyFilter">Flags indicating the types of changes to monitor.</param>
        /// <exception cref="ArgumentException">Thrown if the folder path is invalid or does not exist.</exception>
        public void StartMonitoringFileSystem(string folderPath, bool includeSubdirectories = true, NotifyFilters notifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | NotifyFilters.Size)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TriggerGetterUtil));
            if (!Directory.Exists(folderPath))
            {
                throw new ArgumentException($"Directory not found: '{folderPath}'", nameof(folderPath));
            }

            StopMonitoringFileSystem(); // Stop previous watcher if any

            Debug.WriteLine($"Starting File System Monitoring on: '{folderPath}' (IncludeSubdirs={includeSubdirectories}, Filter={notifyFilter})...");
            try
            {
                _fileSystemWatcher = new FileSystemWatcher(folderPath)
                {
                    NotifyFilter = notifyFilter,
                    IncludeSubdirectories = includeSubdirectories,
                    // Increase buffer size if monitoring high-activity folders to prevent overflows
                    // InternalBufferSize = 65536, // Default is 8192 (8KB)
                };

                // Subscribe to the events
                _fileSystemWatcher.Changed += FileSystemWatcher_ActivityHandler;
                _fileSystemWatcher.Created += FileSystemWatcher_ActivityHandler;
                _fileSystemWatcher.Deleted += FileSystemWatcher_ActivityHandler;
                _fileSystemWatcher.Renamed += FileSystemWatcher_RenamedHandler;
                _fileSystemWatcher.Error += FileSystemWatcher_ErrorHandler; // Important for diagnosing issues

                _fileSystemWatcher.EnableRaisingEvents = true; // Start monitoring

                Debug.WriteLine(" -> File System Watcher Started.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR starting file system monitoring: {ex.GetType().Name} - {ex.Message}");
                StopMonitoringFileSystem(); // Clean up partially created watcher
                // Optionally re-throw or handle differently
            }
        }

        /// <summary>
        /// Stops monitoring the file system.
        /// </summary>
        public void StopMonitoringFileSystem()
        {
            Debug.WriteLine("Stopping File System Monitoring...");
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false; // Stop raising events

                // Unsubscribe from events
                _fileSystemWatcher.Changed -= FileSystemWatcher_ActivityHandler;
                _fileSystemWatcher.Created -= FileSystemWatcher_ActivityHandler;
                _fileSystemWatcher.Deleted -= FileSystemWatcher_ActivityHandler;
                _fileSystemWatcher.Renamed -= FileSystemWatcher_RenamedHandler;
                _fileSystemWatcher.Error -= FileSystemWatcher_ErrorHandler;

                _fileSystemWatcher.Dispose(); // Release resources
                _fileSystemWatcher = null;
                Debug.WriteLine(" -> File System Watcher Stopped and Disposed.");
            }
        }

        // --- 5. Registry Key Monitoring ---

        /// <summary>
        /// Starts monitoring a specific registry key for changes using RegNotifyChangeKeyValue.
        /// Note: This runs on a background thread.
        /// </summary>
        /// <param name="hive">The registry hive (e.g., RegistryHive.CurrentUser).</param>
        /// <param name="subKeyPath">The path to the subkey within the hive (e.g., @"Software\MyApp\Settings").</param>
        /// <param name="watchSubtree">True to monitor the key and all its subkeys; False to monitor only the key itself.</param>
        /// <param name="notifyFilter">Flags indicating the types of changes to monitor (default focuses on value changes).</param>
        /// <exception cref="ArgumentException">Thrown if the subKeyPath is null or empty.</exception>
        /// <exception cref="System.Security.SecurityException">Thrown if the user lacks permissions to open the key.</exception>
        /// <exception cref="IOException">Thrown if the key could not be opened (e.g., does not exist).</exception>
        // *** Method signature now uses the public RegNotifyFilter enum ***
        public void StartMonitoringRegistryKey(RegistryHive hive, string subKeyPath, bool watchSubtree = false, RegNotifyFilter notifyFilter = RegNotifyFilter.REG_NOTIFY_CHANGE_LAST_SET | RegNotifyFilter.REG_NOTIFY_CHANGE_NAME)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TriggerGetterUtil));
            if (string.IsNullOrEmpty(subKeyPath)) throw new ArgumentException("Registry subkey path cannot be null or empty.", nameof(subKeyPath));

            StopMonitoringRegistryKey(); // Ensure previous monitoring is stopped

            Debug.WriteLine($"Starting Registry Monitoring on: {hive}\\{subKeyPath} (WatchSubtree={watchSubtree}, Filter={notifyFilter})...");

            _monitoredHive = hive;
            _monitoredRegistrySubKey = subKeyPath;
            _watchSubtreeRegistry = watchSubtree;
            _registryNotifyFilter = notifyFilter; // Store the filter

            // Get the predefined handle for the specified hive
            _registryHiveHandle = GetHiveHandle(hive);
            if (_registryHiveHandle == IntPtr.Zero)
            {
                Debug.WriteLine($"ERROR starting registry monitoring: Invalid Hive specified '{hive}'");
                throw new ArgumentException($"Invalid or unsupported Registry Hive: {hive}", nameof(hive));
            }

            // Attempt to open the registry key with Notify permissions
            int result = NativeMethods.RegOpenKeyEx(
                _registryHiveHandle,
                subKeyPath,
                0, // ulOptions - reserved, must be zero
                NativeMethods.KEY_NOTIFY, // samDesired - we only need permission to be notified
                out _registryKeyHandle);

            if (result != 0 || _registryKeyHandle == IntPtr.Zero) // 0 is ERROR_SUCCESS
            {
                _registryKeyHandle = IntPtr.Zero; // Ensure it's zeroed if open failed
                // Convert Win32 error code to an exception
                var ex = new Win32Exception(result);
                Debug.WriteLine($"ERROR starting registry monitoring: Could not open key '{hive}\\{subKeyPath}'. Win32 Error Code: {result} ({ex.Message})");
                // Throw appropriate exception based on error code
                if (result == 2) // ERROR_FILE_NOT_FOUND
                    throw new IOException($"Registry key not found: {hive}\\{subKeyPath}", ex);
                if (result == 5) // ERROR_ACCESS_DENIED
                    throw new System.Security.SecurityException($"Access denied opening registry key: {hive}\\{subKeyPath}", ex);
                throw new IOException($"Failed to open registry key: {hive}\\{subKeyPath}", ex);
            }

            Debug.WriteLine($" -> Registry Key Opened (Handle: {_registryKeyHandle}).");

            // Create the event used to signal the monitoring thread to stop
            _registryStopEvent = new ManualResetEvent(false); // Initially not signaled

            // Create and start the background thread for monitoring
            _registryWatcherThread = new Thread(RegistryMonitorThreadWorker)
            {
                Name = $"RegistryMonitor_{hive}_{subKeyPath}",
                IsBackground = true // Allows application to exit even if this thread is running
            };
            _registryWatcherThread.Start();
            Debug.WriteLine(" -> Registry Monitor Thread Started.");
        }

        /// <summary>
        /// Stops monitoring the registry key by signaling the background thread and closing handles.
        /// </summary>
        public void StopMonitoringRegistryKey()
        {
            Debug.WriteLine("Stopping Registry Monitoring...");
            // 1. Signal the monitoring thread to stop
            if (_registryStopEvent != null)
            {
                Debug.WriteLine(" -> Signaling stop event...");
                try
                {
                    _registryStopEvent.Set(); // Signal the event
                }
                catch (ObjectDisposedException) { /* Ignore if already disposed */ }
            }

            // 2. Optionally wait for the thread to finish
            // Note: The thread might be blocked in RegNotifyChangeKeyValue. Closing the key handle
            // below is often necessary to unblock it if it hasn't detected the stop signal yet.
            if (_registryWatcherThread != null && _registryWatcherThread.IsAlive)
            {
                Debug.WriteLine(" -> Waiting for registry thread to join...");
                // Wait for a short period. If it doesn't exit, closing the handle should force it.
                if (!_registryWatcherThread.Join(TimeSpan.FromMilliseconds(500)))
                {
                    Debug.WriteLine(" -> Registry thread did not join in time (likely blocked). Will proceed with handle closing.");
                    // Consider Thread.Interrupt() or Abort() as last resort, but they are risky. Closing handle is preferred.
                }
                else
                {
                    Debug.WriteLine(" -> Registry thread joined successfully.");
                }
            }
            _registryWatcherThread = null; // Clear the thread reference

            // 3. Clean up handles (Closing the key handle often unblocks RegNotifyChangeKeyValue)
            if (_registryKeyHandle != IntPtr.Zero)
            {
                Debug.WriteLine($" -> Closing registry key handle ({_registryKeyHandle})...");
                int closeResult = NativeMethods.RegCloseKey(_registryKeyHandle);
                if (closeResult != 0)
                {
                    Debug.WriteLine($" -> WARNING: RegCloseKey failed with error code {closeResult}.");
                }
                else
                {
                    Debug.WriteLine(" -> Registry key handle closed.");
                }
                _registryKeyHandle = IntPtr.Zero; // Mark as closed
            }
            _registryHiveHandle = IntPtr.Zero; // Hive handle doesn't need closing, it's predefined

            // 4. Dispose the ManualResetEvent
            if (_registryStopEvent != null)
            {
                _registryStopEvent.Dispose();
                _registryStopEvent = null;
                Debug.WriteLine(" -> Stop event disposed.");
            }
            Debug.WriteLine("Registry Monitoring Stopped.");
        }

        #endregion

        #region Internal Event Handlers & Processing Logic

        // --- WMI Process Event Handlers ---

        private void ProcessStartedWmiHandler(object sender, EventArrivedEventArgs e)
        {
            try
            {
                // Extract process information from the WMI event object
                if (!(e.NewEvent["TargetInstance"] is ManagementBaseObject targetInstance)) return;

                uint processId = (uint)targetInstance["ProcessID"];
                string processName = (string)targetInstance["ProcessName"];

                // Getting detailed info (Path, HWND, Title) immediately at start via WMI is unreliable.
                // We attempt to get it shortly after, but some info might be missing or require elevation.
                Debug.WriteLine($"WMI Event: Process Started - PID={processId}, Name='{processName}'");
                ProcessInfo info = GetProcessInfo((int)processId); // Attempt to get more details

                // Raise the public event
                OnApplicationStarted(new ApplicationEventArgs(
                    (int)processId,
                    info.WindowHandle, // Might be IntPtr.Zero if no main window yet/ever
                    processName,       // Name from WMI is reliable
                    info.WindowTitle,  // Might be empty
                    info.ExecutablePath // Might be null if access denied or process exits quickly
                ));
            }
            catch (Exception ex)
            {
                // Log errors during event processing
                Debug.WriteLine($"ERROR processing WMI process start event: {ex.Message}");
            }
            finally
            {
                // WMI objects might need disposal if not handled automatically, but typically EventArrivedEventArgs handles it.
                // ((ManagementBaseObject)e.NewEvent["TargetInstance"])?.Dispose(); // Example if manual disposal needed
            }
        }

        private void ProcessStoppedWmiHandler(object sender, EventArrivedEventArgs e)
        {
            try
            {
                if (!(e.NewEvent["TargetInstance"] is ManagementBaseObject targetInstance)) return;

                uint processId = (uint)targetInstance["ProcessID"];
                string processName = (string)targetInstance["ProcessName"];
                Debug.WriteLine($"WMI Event: Process Stopped - PID={processId}, Name='{processName}'");

                // Process has exited, so HWND, Title, Path are no longer valid/easily accessible.
                // Report only the information readily available from the WMI event.
                OnApplicationStopped(new ApplicationEventArgs(
                    (int)processId,
                    IntPtr.Zero, // Handle is invalid
                    processName,
                    string.Empty, // Title is gone
                    string.Empty  // Path is not relevant/retrievable easily
                ));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR processing WMI process stop event: {ex.Message}");
            }
            finally
            {
                // ((ManagementBaseObject)e.NewEvent["TargetInstance"])?.Dispose();
            }
        }

        // --- WinEvent Hook Callback ---

        /// <summary>
        /// Callback method invoked by the system when a hooked window event occurs.
        /// This method runs on a system thread, NOT the main UI thread.
        /// </summary>
        private void WinEventProcCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Filter out events for non-windows or system components if necessary.
            // hwnd == IntPtr.Zero usually means the event is not associated with a specific window (e.g., desktop).
            // idObject != 0 (OBJID_WINDOW) means it's not the window itself but a child object.
            // idChild != 0 (CHILDID_SELF) means it's a child element within the object.
            // For Activation and Move/Size, we are typically interested in the main window (hwnd != 0, idObject=0, idChild=0).
            if (hwnd == IntPtr.Zero) // || idObject != 0 || idChild != 0)
            {
                // Debug.WriteLine($"WinEventProcCallback ignored: HWND={hwnd}, Event={eventType}, idObject={idObject}, idChild={idChild}");
                return;
            }

            // Check if the window handle is still valid (optional, might add overhead)
            // if (!IsWindow(hwnd)) return; // Requires P/Invoke for IsWindow

            Debug.WriteLine($"WinEventProcCallback: HWND={hwnd}, Event={eventType}, idObject={idObject}, idChild={idChild}");

            // Get process information associated with the window handle
            ProcessInfo info = GetProcessInfoFromHWND(hwnd);
            if (info.ProcessId == 0)
            {
                Debug.WriteLine($" -> Could not get process info for HWND {hwnd}. Ignoring event.");
                return; // Could not get process info (e.g., window closed between event and processing)
            }

            // --- Handle Specific Event Types ---
            try
            {
                if (eventType == NativeMethods.EVENT_SYSTEM_FOREGROUND)
                {
                    Debug.WriteLine($" -> Foreground Event: PID={info.ProcessId}, Name='{info.ProcessName}', Title='{info.WindowTitle}'");
                    // Raise the ApplicationActivated event
                    OnApplicationActivated(new ApplicationEventArgs(
                        info.ProcessId, info.WindowHandle, info.ProcessName, info.WindowTitle, info.ExecutablePath));
                }
                else if (eventType == NativeMethods.EVENT_SYSTEM_MOVESIZEEND)
                {
                    // Get the window's final position and size
                    if (NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT rect))
                    {
                        int width = rect.Right - rect.Left;
                        int height = rect.Bottom - rect.Top;
                        Debug.WriteLine($" -> MoveSizeEnd Event: PID={info.ProcessId}, Title='{info.WindowTitle}', Rect=({rect.Left},{rect.Top} - {width}x{height})");
                        // Raise the WindowMovedOrResized event
                        OnWindowMovedOrResized(new WindowLocationEventArgs(
                            info.ProcessId, info.WindowHandle, info.ProcessName, info.WindowTitle, info.ExecutablePath,
                            rect.Left, rect.Top, width, height));
                    }
                    else
                    {
                        // Failed to get window rect - maybe window closed? Raise event with invalid coords.
                        Debug.WriteLine($" -> MoveSizeEnd Event: PID={info.ProcessId}, Title='{info.WindowTitle}', GetWindowRect FAILED (Error: {Marshal.GetLastWin32Error()})");
                        OnWindowMovedOrResized(new WindowLocationEventArgs(
                            info.ProcessId, info.WindowHandle, info.ProcessName, info.WindowTitle, info.ExecutablePath,
                            -1, -1, -1, -1)); // Indicate invalid location
                    }
                }
            }
            catch (Exception ex)
            {
                // Catch exceptions during event processing/raising
                Debug.WriteLine($"ERROR processing WinEvent {eventType} for HWND {hwnd}: {ex.Message}");
            }
        }


        // --- File System Event Handlers ---

        // Common handler for Created, Deleted, Changed events
        private void FileSystemWatcher_ActivityHandler(object sender, System.IO.FileSystemEventArgs e)
        {
            try
            {
                Debug.WriteLine($"File System Event: Type={e.ChangeType}, Path='{e.FullPath}', Name='{e.Name}'");
                // Raise the public event
                OnFileSystemChanged(new FileSystemActivityEventArgs(e.ChangeType, e.FullPath, e.Name));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR processing FileSystem event ({e.ChangeType}): {ex.Message}");
            }
        }

        // Specific handler for Renamed events
        private void FileSystemWatcher_RenamedHandler(object sender, RenamedEventArgs e)
        {
            try
            {
                Debug.WriteLine($"File System Event: Type={e.ChangeType}, Path='{e.FullPath}', Name='{e.Name}', OldPath='{e.OldFullPath}', OldName='{e.OldName}'");
                // Raise the public event with old path/name information
                OnFileSystemChanged(new FileSystemActivityEventArgs(e.ChangeType, e.FullPath, e.Name, e.OldFullPath, e.OldName));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR processing FileSystem event ({e.ChangeType}): {ex.Message}");
            }
        }

        // Handler for errors from the FileSystemWatcher itself (e.g., buffer overflow)
        private void FileSystemWatcher_ErrorHandler(object sender, ErrorEventArgs e)
        {
            Exception error = e.GetException();
            Debug.WriteLine($"ERROR in FileSystemWatcher: {error.GetType().Name} - {error.Message}");
            // Consider logging the full exception details: error.ToString()
            // A common issue is internal buffer overflow if many events occur rapidly.
            // You might need to increase InternalBufferSize or handle the error by
            // potentially stopping and restarting the watcher, or notifying the user.
        }


        // --- Registry Monitor Thread Worker ---

        /// <summary>
        /// This method runs on a dedicated background thread.
        /// It blocks waiting for registry change notifications.
        /// </summary>
        private void RegistryMonitorThreadWorker()
        {
            Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}) started for {_monitoredHive}\\{_monitoredRegistrySubKey}.");
            ManualResetEvent stopEvent = _registryStopEvent; // Local copy for thread safety
            IntPtr keyHandle = _registryKeyHandle;          // Local copy for thread safety

            if (stopEvent == null || keyHandle == IntPtr.Zero)
            {
                Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): Invalid state (stopEvent or keyHandle is null/zero). Exiting.");
                return;
            }

            try
            {
                while (true) // Loop indefinitely until stop is signaled or an error occurs
                {
                    // Check if stop has been requested *before* blocking
                    if (stopEvent.WaitOne(0)) // Check immediately, don't block
                    {
                        Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): Stop signal detected before waiting. Exiting.");
                        break;
                    }

                    // Use the local copy of the handle
                    if (keyHandle == IntPtr.Zero)
                    {
                        Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): Key handle is zero before wait. Exiting.");
                        break; // Handle was likely closed by Stop
                    }

                    Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): Calling RegNotifyChangeKeyValue...");

                    // Wait for a change notification. This call BLOCKS the thread.
                    // Pass IntPtr.Zero for hEvent to wait synchronously on this thread.
                    // *** Use the public RegNotifyFilter enum value stored in the field ***
                    int result = NativeMethods.RegNotifyChangeKeyValue(
                        keyHandle,
                        _watchSubtreeRegistry,
                        _registryNotifyFilter, // Use the stored filter value
                        IntPtr.Zero, // hEvent - NULL for synchronous wait on the key handle itself
                        false        // fAsynchronous - Must be FALSE if hEvent is NULL
                    );

                    Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): RegNotifyChangeKeyValue returned {result}.");

                    // After RegNotifyChangeKeyValue returns, check again if stop was requested *while* we were waiting.
                    // The ManualResetEvent might have been signaled by StopMonitoringRegistryKey.
                    if (stopEvent.WaitOne(0))
                    {
                        Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): Stop signal detected after wait. Exiting.");
                        break; // Exit the loop if stop was requested
                    }

                    // Check the result of RegNotifyChangeKeyValue
                    if (result == 0) // 0 = ERROR_SUCCESS -> A change occurred
                    {
                        Debug.WriteLine($" -> Registry change detected under: {_monitoredHive}\\{_monitoredRegistrySubKey}");
                        // Raise the public event. IMPORTANT: This runs on the background thread!
                        // UI updates must be marshaled by the subscriber.
                        OnRegistryValueChanged(new RegistryKeyChangedEventArgs(_monitoredHive, _monitoredRegistrySubKey));

                        // The notification is a one-shot deal. We need to loop
                        // and call RegNotifyChangeKeyValue again to wait for the *next* change.
                        // The loop handles this automatically.
                    }
                    else // An error occurred
                    {
                        // Common errors:
                        // 6 (ERROR_INVALID_HANDLE) - Key handle was closed (likely by StopMonitoring).
                        // 2 (ERROR_FILE_NOT_FOUND) / 1019 (ERROR_KEY_DELETED) - Monitored key was deleted.
                        // 5 (ERROR_ACCESS_DENIED) - Permissions changed.
                        var ex = new Win32Exception(result);
                        Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): RegNotifyChangeKeyValue failed. Win32 Error Code: {result} ({ex.Message}). Stopping monitoring.");
                        // Assume monitoring cannot continue, break the loop.
                        break;
                    }
                } // End while loop
            }
            catch (ObjectDisposedException)
            {
                // Expected if the ManualResetEvent or Key Handle is disposed while the thread is running (e.g., during shutdown)
                Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): ObjectDisposedException caught (likely during shutdown). Exiting.");
            }
            catch (Exception ex)
            {
                // Catch unexpected exceptions on the monitoring thread
                Debug.WriteLine($"FATAL ERROR in Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}): {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                // Consider logging this more formally
            }
            finally
            {
                Debug.WriteLine($"Registry Monitor Thread ({Thread.CurrentThread.ManagedThreadId}) finished.");
                // Ensure the stop event is set if the loop exits unexpectedly, allowing Stop method to potentially join.
                try { stopEvent?.Set(); } catch (ObjectDisposedException) { /* Ignore */ }
                // Do NOT close the key handle here; StopMonitoringRegistryKey is responsible for that.
            }
        }


        #endregion

        #region Protected Event Raiser Methods

        // These methods provide a standard way to raise events, checking for subscribers.
        // They can be overridden in derived classes if needed.

        protected virtual void OnApplicationStarted(ApplicationEventArgs e)
        {
            // Make a temporary copy of the event to be thread-safe.
            EventHandler<ApplicationEventArgs> handler = ApplicationStarted;
            handler?.Invoke(this, e);
        }

        protected virtual void OnApplicationStopped(ApplicationEventArgs e)
        {
            EventHandler<ApplicationEventArgs> handler = ApplicationStopped;
            handler?.Invoke(this, e);
        }

        protected virtual void OnApplicationActivated(ApplicationEventArgs e)
        {
            EventHandler<ApplicationEventArgs> handler = ApplicationActivated;
            handler?.Invoke(this, e);
        }

        protected virtual void OnWindowMovedOrResized(WindowLocationEventArgs e)
        {
            EventHandler<WindowLocationEventArgs> handler = WindowMovedOrResized;
            handler?.Invoke(this, e);
        }

        protected virtual void OnFileSystemChanged(FileSystemActivityEventArgs e)
        {
            EventHandler<FileSystemActivityEventArgs> handler = FileSystemChanged;
            handler?.Invoke(this, e);
        }

        protected virtual void OnRegistryValueChanged(RegistryKeyChangedEventArgs e)
        {
            EventHandler<RegistryKeyChangedEventArgs> handler = RegistryValueChanged;
            handler?.Invoke(this, e);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Helper struct to bundle process information.
        /// </summary>
        private struct ProcessInfo
        {
            public int ProcessId;
            public IntPtr WindowHandle;
            public string ProcessName;
            public string WindowTitle;
            public string ExecutablePath; // Can be null if access denied or process exited
        }

        /// <summary>
        /// Gets process information (PID, Name, Path, Title) based on a window handle (HWND).
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <returns>A ProcessInfo struct. ProcessId will be 0 if info cannot be retrieved.</returns>
        private ProcessInfo GetProcessInfoFromHWND(IntPtr hwnd)
        {
            var info = new ProcessInfo { WindowHandle = hwnd };
            if (hwnd == IntPtr.Zero) return info;

            try
            {
                // 1. Get Process ID from HWND
                uint processId = 0;
                NativeMethods.GetWindowThreadProcessId(hwnd, out processId);
                info.ProcessId = (int)processId;
                if (processId == 0)
                {
                    Debug.WriteLine($"GetProcessInfoFromHWND: Failed to get PID for HWND {hwnd}.");
                    return info; // Failed to get PID
                }

                // 2. Get Process Object from PID (can fail if process exited)
                using (Process proc = Process.GetProcessById(info.ProcessId))
                {
                    info.ProcessName = proc.ProcessName;

                    // 3. Get Executable Path (can fail due to permissions or bitness mismatch)
                    try
                    {
                        // Accessing MainModule can throw Win32Exception (access denied) or InvalidOperationException (process exited)
                        info.ExecutablePath = proc.MainModule?.FileName;
                    }
                    catch (Win32Exception w32ex) when (w32ex.NativeErrorCode == 5 || w32ex.NativeErrorCode == 299) // 5=Access Denied, 299=Only part of read/write completed (often 32/64 bit issue)
                    {
                        Debug.WriteLine($"GetProcessInfoFromHWND: Access denied/bitness issue getting path for PID {info.ProcessId} via MainModule (Error {w32ex.NativeErrorCode}). Trying fallback...");
                        info.ExecutablePath = GetProcessExecutablePathPInvoke(info.ProcessId);
                    }
                    catch (InvalidOperationException ioex)
                    {
                        // Process likely exited between getting PID and accessing MainModule
                        Debug.WriteLine($"GetProcessInfoFromHWND: Process {info.ProcessId} likely exited before path retrieval: {ioex.Message}");
                        info.ExecutablePath = null; // Indicate path is unavailable
                    }
                    catch (Exception ex) // Catch other potential errors
                    {
                        Debug.WriteLine($"GetProcessInfoFromHWND: Unexpected error getting MainModule for PID {info.ProcessId}: {ex.Message}. Trying fallback...");
                        info.ExecutablePath = GetProcessExecutablePathPInvoke(info.ProcessId); // Try fallback anyway
                    }
                } // using Process automatically disposes it

                // 4. Get Window Title (using P/Invoke is often more reliable than proc.MainWindowTitle)
                int length = NativeMethods.GetWindowTextLength(hwnd);
                if (length > 0)
                {
                    StringBuilder titleBuilder = new StringBuilder(length + 1);
                    if (NativeMethods.GetWindowText(hwnd, titleBuilder, titleBuilder.Capacity) > 0)
                    {
                        info.WindowTitle = titleBuilder.ToString();
                    }
                    else
                    {
                        Debug.WriteLine($"GetProcessInfoFromHWND: GetWindowText failed for HWND {hwnd}, PID {info.ProcessId} (Error: {Marshal.GetLastWin32Error()}).");
                    }
                }
                else
                {
                    // Window might genuinely have no title, or GetWindowTextLength failed
                    info.WindowTitle = string.Empty;
                }
            }
            catch (ArgumentException argEx)
            {
                // Process.GetProcessById throws this if the process has exited
                Debug.WriteLine($"GetProcessInfoFromHWND: Process {info.ProcessId} likely exited: {argEx.Message}");
                info.ProcessId = 0; // Mark as failed
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR in GetProcessInfoFromHWND for HWND {hwnd}: {ex.GetType().Name} - {ex.Message}");
                // Optionally reset fields on error
                // info.ProcessId = 0;
            }
            return info;
        }

        /// <summary>
        /// Gets process information directly from a Process ID.
        /// Less reliable for HWND/Title as they might not exist when called (e.g., at process start).
        /// </summary>
        private ProcessInfo GetProcessInfo(int processId)
        {
            var info = new ProcessInfo { ProcessId = processId };
            try
            {
                using (Process proc = Process.GetProcessById(processId))
                {
                    info.ProcessName = proc.ProcessName;
                    // These might be invalid/unavailable, especially at process start
                    try { info.WindowHandle = proc.MainWindowHandle; } catch { info.WindowHandle = IntPtr.Zero; }
                    try { info.WindowTitle = proc.MainWindowTitle; } catch { info.WindowTitle = string.Empty; }

                    // Get Executable Path (handle potential exceptions)
                    try
                    {
                        info.ExecutablePath = proc.MainModule?.FileName;
                    }
                    catch (Win32Exception w32ex) when (w32ex.NativeErrorCode == 5 || w32ex.NativeErrorCode == 299)
                    {
                        Debug.WriteLine($"GetProcessInfo (PID): Access denied/bitness issue getting path for PID {info.ProcessId} via MainModule (Error {w32ex.NativeErrorCode}). Trying fallback...");
                        info.ExecutablePath = GetProcessExecutablePathPInvoke(info.ProcessId);
                    }
                    catch (InvalidOperationException ioex)
                    {
                        Debug.WriteLine($"GetProcessInfo (PID): Process {info.ProcessId} likely exited before path retrieval: {ioex.Message}");
                        info.ExecutablePath = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"GetProcessInfo (PID): Unexpected error getting MainModule for PID {info.ProcessId}: {ex.Message}. Trying fallback...");
                        info.ExecutablePath = GetProcessExecutablePathPInvoke(info.ProcessId);
                    }
                }
            }
            catch (ArgumentException)
            {
                Debug.WriteLine($"GetProcessInfo (PID): Process {processId} likely exited before GetProcessById.");
                // Return info with only PID set
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting process info for PID {processId}: {ex.Message}");
            }
            return info;
        }

        /// <summary>
        /// Fallback method to get process executable path using P/Invoke (OpenProcess, GetModuleFileNameEx).
        /// Often more reliable than Process.MainModule.FileName, especially across bitness or with elevated processes.
        /// </summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>The full path to the executable, or null if retrieval fails.</returns>
        private string GetProcessExecutablePathPInvoke(int processId)
        {
            IntPtr processHandle = IntPtr.Zero;
            try
            {
                // Request minimal permissions needed to query the path
                processHandle = NativeMethods.OpenProcess(NativeMethods.ProcessAccessFlags.QueryLimitedInformation, false, (uint)processId);
                if (processHandle == IntPtr.Zero)
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    // Common error: 5 (Access Denied), 87 (Invalid Parameter - process likely exited)
                    Debug.WriteLine($"GetProcessExecutablePathPInvoke: Failed to OpenProcess for PID {processId}. Win32 Error: {errorCode}");
                    return null;
                }

                StringBuilder buffer = new StringBuilder(1024); // MAX_PATH is 260, but use larger buffer just in case
                uint size = NativeMethods.GetModuleFileNameEx(processHandle, IntPtr.Zero, buffer, buffer.Capacity);

                if (size > 0)
                {
                    return buffer.ToString();
                }
                else
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    // Common error: 299 (Only part of read/write completed - often buffer too small, though unlikely here)
                    Debug.WriteLine($"GetProcessExecutablePathPInvoke: Failed to GetModuleFileNameEx for PID {processId}. Win32 Error: {errorCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Catch unexpected errors during P/Invoke calls
                Debug.WriteLine($"GetProcessExecutablePathPInvoke: Exception for PID {processId}: {ex.Message}");
                return null;
            }
            finally
            {
                // IMPORTANT: Always close the process handle obtained via OpenProcess
                if (processHandle != IntPtr.Zero)
                {
                    NativeMethods.CloseHandle(processHandle);
                }
            }
        }

        /// <summary>
        /// Maps a RegistryHive enum value to its corresponding predefined native handle.
        /// </summary>
        private IntPtr GetHiveHandle(RegistryHive hive)
        {
            switch (hive)
            {
                case RegistryHive.ClassesRoot: return NativeMethods.HKEY_CLASSES_ROOT;
                case RegistryHive.CurrentUser: return NativeMethods.HKEY_CURRENT_USER;
                case RegistryHive.LocalMachine: return NativeMethods.HKEY_LOCAL_MACHINE;
                case RegistryHive.Users: return NativeMethods.HKEY_USERS;
                case RegistryHive.CurrentConfig: return NativeMethods.HKEY_CURRENT_CONFIG;
                // PerformanceData and DynData are less common for direct monitoring via RegNotifyChangeKeyValue
                // case RegistryHive.PerformanceData: return NativeMethods.HKEY_PERFORMANCE_DATA;
                default:
                    Debug.WriteLine($"GetHiveHandle: Unsupported or invalid RegistryHive value: {hive}");
                    return IntPtr.Zero; // Indicate unsupported hive
            }
        }

        #endregion
    }
}
