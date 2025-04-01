using Microsoft.Win32;
using StatusGetter;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing camera information
    /// </summary>
    public interface ICameraInfo
    {
        /// <summary>
        /// Checks if the camera is currently in use by any application
        /// </summary>
        /// <returns>True if camera is in use, false otherwise</returns>
        bool IsCameraInUse();

        /// <summary>
        /// Gets information about all applications that can access the camera
        /// </summary>
        /// <returns>List of camera access information for applications</returns>
        List<CameraAccessInfo> GetCameraApplications();

        /// <summary>
        /// Gets information about applications currently using the camera
        /// </summary>
        /// <returns>List of camera access information for applications currently using the camera</returns>
        List<CameraAccessInfo> GetActivelyUsingApplications();

        /// <summary>
        /// Starts monitoring camera usage changes
        /// </summary>
        /// <param name="callback">Callback to execute when camera usage changes</param>
        void StartMonitoring(Action<CameraUsageChangedEventArgs> callback);

        /// <summary>
        /// Stops monitoring camera usage changes
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// Gets the last time camera was used by any application
        /// </summary>
        /// <returns>DateTime representing the last camera usage, or null if never used</returns>
        DateTime? GetLastCameraUsageTime();
    }

    /// <summary>
    /// Contains information about camera access for an application
    /// </summary>
    public class CameraAccessInfo
    {
        /// <summary>
        /// Application key name in registry (e.g., "MSTeams_8wekyb3d8bbwe")
        /// </summary>
        public string ApplicationKeyName { get; set; }

        /// <summary>
        /// Friendly application name (if available)
        /// </summary>
        public string ApplicationFriendlyName { get; set; }

        /// <summary>
        /// Time when the application started using the camera
        /// </summary>
        public DateTime? LastUsedTimeStart { get; set; }

        /// <summary>
        /// Time when the application stopped using the camera
        /// </summary>
        public DateTime? LastUsedTimeStop { get; set; }

        /// <summary>
        /// Whether the application is currently using the camera
        /// </summary>
        public bool IsCurrentlyUsing { get; set; }

        /// <summary>
        /// Returns a string representation of the camera access information
        /// </summary>
        public override string ToString()
        {
            string status = IsCurrentlyUsing ? "Currently Using" : "Not Using";
            string appName = !string.IsNullOrEmpty(ApplicationFriendlyName)
                ? ApplicationFriendlyName
                : ApplicationKeyName;

            return $"{appName}: {status} (Started: {LastUsedTimeStart})";
        }
    }

    /// <summary>
    /// Event arguments for camera usage changed events
    /// </summary>
    public class CameraUsageChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether the camera is now in use
        /// </summary>
        public bool IsInUse { get; set; }

        /// <summary>
        /// List of applications currently using the camera
        /// </summary>
        public List<CameraAccessInfo> UsingApplications { get; set; }

        /// <summary>
        /// Application that triggered the change (started/stopped using camera)
        /// </summary>
        public CameraAccessInfo ChangedApplication { get; set; }

        /// <summary>
        /// Type of change (started using or stopped using)
        /// </summary>
        public CameraUsageChangeType ChangeType { get; set; }
    }

    /// <summary>
    /// Type of camera usage change
    /// </summary>
    public enum CameraUsageChangeType
    {
        /// <summary>
        /// An application started using the camera
        /// </summary>
        Started,

        /// <summary>
        /// An application stopped using the camera
        /// </summary>
        Stopped
    }

    /// <summary>
    /// Provides information about camera usage by applications
    /// </summary>
    public class CameraInfo : ICameraInfo, IDisposable
    {
        // P/Invoke declarations for registry notification
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegNotifyChangeKeyValue(
            IntPtr hKey,
            bool bWatchSubtree,
            RegNotifyFilter dwNotifyFilter,
            IntPtr hEvent,
            bool fAsynchronous);

        [Flags]
        private enum RegNotifyFilter
        {
            Name = 1,
            Attributes = 2,
            LastSet = 4,
            Security = 8,
            Value = 1
        }

        // Registry paths
        private const string CameraConsentStorePath = @"Software\Microsoft\Windows\CurrentVersion\CapabilityAccessManager\ConsentStore\webcam";

        // Monitoring fields
        private bool _isMonitoring;
        private Task _monitoringTask;
        private CancellationTokenSource _cancellationTokenSource;
        private Action<CameraUsageChangedEventArgs> _callback;

        // Registry cache to track changes
        private Dictionary<string, CameraAccessInfo> _lastKnownState;

        /// <summary>
        /// Initializes a new instance of the CameraInfo class
        /// </summary>
        public CameraInfo()
        {
            _lastKnownState = new Dictionary<string, CameraAccessInfo>();
        }

        /// <summary>
        /// Checks if the camera is currently in use by any application
        /// </summary>
        /// <returns>True if camera is in use, false otherwise</returns>
        public bool IsCameraInUse()
        {
            var activeApps = GetActivelyUsingApplications();
            return activeApps.Count > 0;
        }

        /// <summary>
        /// Gets information about all applications that can access the camera
        /// </summary>
        /// <returns>List of camera access information for applications</returns>
        public List<CameraAccessInfo> GetCameraApplications()
        {
            List<CameraAccessInfo> applications = new List<CameraAccessInfo>();

            try
            {
                using (RegistryKey webcamKey = Registry.CurrentUser.OpenSubKey(CameraConsentStorePath))
                {
                    if (webcamKey == null)
                        return applications;

                    foreach (string appKeyName in webcamKey.GetSubKeyNames())
                    {
                        using (RegistryKey appKey = webcamKey.OpenSubKey(appKeyName))
                        {
                            if (appKey != null)
                            {
                                var accessInfo = ExtractCameraAccessInfo(appKey, appKeyName);
                                if (accessInfo != null)
                                {
                                    applications.Add(accessInfo);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting camera applications: {ex.Message}");
            }

            return applications;
        }

        /// <summary>
        /// Gets information about applications currently using the camera
        /// </summary>
        /// <returns>List of camera access information for applications currently using the camera</returns>
        public List<CameraAccessInfo> GetActivelyUsingApplications()
        {
            return GetCameraApplications().FindAll(app => app.IsCurrentlyUsing);
        }

        /// <summary>
        /// Gets the last time camera was used by any application
        /// </summary>
        /// <returns>DateTime representing the last camera usage, or null if never used</returns>
        public DateTime? GetLastCameraUsageTime()
        {
            DateTime? lastUsage = null;

            foreach (var app in GetCameraApplications())
            {
                // For currently using apps, use the start time
                if (app.IsCurrentlyUsing && app.LastUsedTimeStart.HasValue)
                {
                    if (!lastUsage.HasValue || app.LastUsedTimeStart.Value > lastUsage.Value)
                    {
                        lastUsage = app.LastUsedTimeStart.Value;
                    }
                }
                // For apps not currently using, use the stop time
                else if (app.LastUsedTimeStop.HasValue)
                {
                    if (!lastUsage.HasValue || app.LastUsedTimeStop.Value > lastUsage.Value)
                    {
                        lastUsage = app.LastUsedTimeStop.Value;
                    }
                }
            }

            return lastUsage;
        }

        /// <summary>
        /// Starts monitoring camera usage changes
        /// </summary>
        /// <param name="callback">Callback to execute when camera usage changes</param>
        public void StartMonitoring(Action<CameraUsageChangedEventArgs> callback)
        {
            if (_isMonitoring)
                return;

            _callback = callback;
            _isMonitoring = true;
            _cancellationTokenSource = new CancellationTokenSource();
            _lastKnownState = BuildApplicationStateCache();

            _monitoringTask = Task.Run(async () =>
            {
                while (_isMonitoring && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    CheckForCameraUsageChanges();
                    await Task.Delay(500, _cancellationTokenSource.Token); // Check every 500ms
                }
            }, _cancellationTokenSource.Token);
        }

        /// <summary>
        /// Stops monitoring camera usage changes
        /// </summary>
        public void StopMonitoring()
        {
            _isMonitoring = false;
            _cancellationTokenSource?.Cancel();
            _monitoringTask = null;
            _callback = null;
        }

        /// <summary>
        /// Disposes resources used by the CameraInfo class
        /// </summary>
        public void Dispose()
        {
            StopMonitoring();
        }

        #region Helper Methods

        /// <summary>
        /// Extracts camera access information from a registry key
        /// </summary>
        private CameraAccessInfo ExtractCameraAccessInfo(RegistryKey appKey, string appKeyName)
        {
            try
            {
                // Try to get LastUsedTimeStart and LastUsedTimeStop values
                long lastUsedTimeStart = 0;
                long lastUsedTimeStop = 0;

                object startObj = appKey.GetValue("LastUsedTimeStart");
                if (startObj != null && startObj is byte[] startBytes)
                {
                    lastUsedTimeStart = BitConverter.ToInt64(startBytes, 0);
                }

                object stopObj = appKey.GetValue("LastUsedTimeStop");
                if (stopObj != null && stopObj is byte[] stopBytes)
                {
                    lastUsedTimeStop = BitConverter.ToInt64(stopBytes, 0);
                }

                // Create CameraAccessInfo object
                CameraAccessInfo accessInfo = new CameraAccessInfo
                {
                    ApplicationKeyName = appKeyName,
                    ApplicationFriendlyName = GetFriendlyAppName(appKeyName),
                    LastUsedTimeStart = lastUsedTimeStart > 0 ? FileTimeToDateTime(lastUsedTimeStart) : (DateTime?)null,
                    LastUsedTimeStop = lastUsedTimeStop > 0 ? FileTimeToDateTime(lastUsedTimeStop) : (DateTime?)null
                };

                // Determine if the application is currently using the camera
                // If LastUsedTimeStop is 0 and LastUsedTimeStart has a value, camera is in use
                accessInfo.IsCurrentlyUsing = lastUsedTimeStart > 0 && lastUsedTimeStop == 0;

                return accessInfo;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting camera access info for {appKeyName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts a FILETIME value to a DateTime
        /// </summary>
        private DateTime FileTimeToDateTime(long fileTime)
        {
            return DateTime.FromFileTime(fileTime);
        }

        /// <summary>
        /// Attempts to get a friendly application name from the registry key name
        /// </summary>
        private string GetFriendlyAppName(string appKeyName)
        {
            // Extract application name from the key
            // Examples:
            // "MSTeams_8wekyb3d8bbwe" -> "Microsoft Teams"
            // "Zoom" -> "Zoom"
            // "Microsoft.SkypeApp_kzf8qxf38zg5c" -> "Skype"

            // Simple parsing rules - could be expanded for a more comprehensive solution
            string friendlyName = appKeyName;

            // Remove the package ID suffix if present
            int underscoreIndex = appKeyName.IndexOf('_');
            if (underscoreIndex > 0)
            {
                friendlyName = appKeyName.Substring(0, underscoreIndex);
            }

            // Handle common prefixes
            if (friendlyName.StartsWith("Microsoft."))
            {
                friendlyName = friendlyName.Substring(10);
            }

            // Remove "App" suffix if present
            if (friendlyName.EndsWith("App"))
            {
                friendlyName = friendlyName.Substring(0, friendlyName.Length - 3);
            }

            // Replace specific known app IDs with friendly names
            switch (friendlyName.ToLower())
            {
                case "msteams": return "Microsoft Teams";
                case "skype": return "Skype";
                case "zoom": return "Zoom";
                case "slack": return "Slack";
                case "firefox": return "Firefox";
                case "chrome": return "Google Chrome";
                case "msedge": return "Microsoft Edge";
                case "webex": return "Cisco Webex";
                default: return friendlyName;
            }
        }

        /// <summary>
        /// Builds a cache of current application states for change detection
        /// </summary>
        private Dictionary<string, CameraAccessInfo> BuildApplicationStateCache()
        {
            var cache = new Dictionary<string, CameraAccessInfo>();
            var apps = GetCameraApplications();

            foreach (var app in apps)
            {
                cache[app.ApplicationKeyName] = app;
            }

            return cache;
        }

        /// <summary>
        /// Checks for changes in camera usage and triggers callback if changes are detected
        /// </summary>
        private void CheckForCameraUsageChanges()
        {
            try
            {
                var currentState = BuildApplicationStateCache();
                CameraAccessInfo changedApp = null;
                CameraUsageChangeType changeType = CameraUsageChangeType.Started;

                // Look for apps that weren't using the camera before but are now
                foreach (var appKey in currentState.Keys)
                {
                    var currentApp = currentState[appKey];

                    if (_lastKnownState.TryGetValue(appKey, out var previousApp))
                    {
                        // App was not using camera before but is now
                        if (!previousApp.IsCurrentlyUsing && currentApp.IsCurrentlyUsing)
                        {
                            changedApp = currentApp;
                            changeType = CameraUsageChangeType.Started;
                            break;
                        }
                        // App was using camera before but is not now
                        else if (previousApp.IsCurrentlyUsing && !currentApp.IsCurrentlyUsing)
                        {
                            changedApp = currentApp;
                            changeType = CameraUsageChangeType.Stopped;
                            break;
                        }
                    }
                    // New app that we haven't seen before and is using the camera
                    else if (currentApp.IsCurrentlyUsing)
                    {
                        changedApp = currentApp;
                        changeType = CameraUsageChangeType.Started;
                        break;
                    }
                }

                // Check for apps that were using the camera but are no longer in the registry
                if (changedApp == null)
                {
                    foreach (var appKey in _lastKnownState.Keys)
                    {
                        if (!currentState.ContainsKey(appKey) && _lastKnownState[appKey].IsCurrentlyUsing)
                        {
                            changedApp = _lastKnownState[appKey];
                            changeType = CameraUsageChangeType.Stopped;
                            break;
                        }
                    }
                }

                // If we found a change, trigger the callback
                if (changedApp != null && _callback != null)
                {
                    var activeApps = new List<CameraAccessInfo>();
                    foreach (var app in currentState.Values)
                    {
                        if (app.IsCurrentlyUsing)
                            activeApps.Add(app);
                    }

                    _callback(new CameraUsageChangedEventArgs
                    {
                        IsInUse = activeApps.Count > 0,
                        UsingApplications = activeApps,
                        ChangedApplication = changedApp,
                        ChangeType = changeType
                    });
                }

                // Update the cache
                _lastKnownState = currentState;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking for camera usage changes: {ex.Message}");
            }
        }

        #endregion
    }
}



//// Example: Check if camera is in use
//ICameraInfo cameraInfo = new CameraInfo();
//bool isInUse = cameraInfo.IsCameraInUse();
//Console.WriteLine($"Camera is currently in use: {isInUse}");

//// Example: Get all applications using the camera
//List<CameraAccessInfo> activeApps = cameraInfo.GetActivelyUsingApplications();
//Console.WriteLine($"Number of applications using camera: {activeApps.Count}");
//foreach (var app in activeApps)
//{
//    Console.WriteLine(app.ToString());
//}

//// Example: Start monitoring for camera usage changes
//cameraInfo.StartMonitoring((args) => {
//    if (args.ChangeType == CameraUsageChangeType.Started)
//    {
//        Console.WriteLine($"{args.ChangedApplication.ApplicationFriendlyName} started using the camera");
//    }
//    else
//    {
//        Console.WriteLine($"{args.ChangedApplication.ApplicationFriendlyName} stopped using the camera");
//    }

//    Console.WriteLine($"Camera in use: {args.IsInUse}");
//});

//// Later:
//cameraInfo.StopMonitoring();
//((IDisposable)cameraInfo).Dispose();


