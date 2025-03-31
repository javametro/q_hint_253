using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusGetter
{
    /// <summary>
    /// Interface for application installation status operations
    /// </summary>
    public interface IApplicationInstallStatus
    {
        /// <summary>
        /// Gets a list of AUMIDs (Application User Model IDs) for installed applications
        /// </summary>
        /// <returns>A list of AUMID strings</returns>
        List<string> GetInstalledApplicationAumids();
    }

    //アプリケーションのインストール情報
    public class ApplicationInstallStatus : IApplicationInstallStatus
    {
        /// <summary>
        /// Gets a list of AUMIDs (Application User Model IDs) for installed applications
        /// </summary>
        /// <returns>A list of AUMID strings</returns>
        public List<string> GetInstalledApplicationAumids()
        {
            List<string> aumids = new List<string>();

            try
            {
                // Get modern Windows apps (UWP/Store apps)
                string packagesKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModel\StateRepository\Cache\Package";
                using (RegistryKey packagesKey = Registry.LocalMachine.OpenSubKey(packagesKeyPath))
                {
                    if (packagesKey != null)
                    {
                        foreach (string subKeyName in packagesKey.GetSubKeyNames())
                        {
                            using (RegistryKey packageKey = packagesKey.OpenSubKey(subKeyName))
                            {
                                if (packageKey != null)
                                {
                                    string packageFullName = packageKey.GetValue("PackageFullName") as string;
                                    if (!string.IsNullOrEmpty(packageFullName))
                                    {
                                        string applicationKeyPath = $@"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages\{packageFullName}\App";
                                        using (RegistryKey appKey = Registry.CurrentUser.OpenSubKey(applicationKeyPath))
                                        {
                                            if (appKey != null)
                                            {
                                                foreach (string appId in appKey.GetSubKeyNames())
                                                {
                                                    string aumid = $"{packageFullName}!{appId}";
                                                    aumids.Add(aumid);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Get traditional desktop apps with AppUserModelIDs
                using (RegistryKey registeredAppsKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\ApplicationAssociationToasts"))
                {
                    if (registeredAppsKey != null)
                    {
                        foreach (string name in registeredAppsKey.GetValueNames())
                        {
                            if (name.Contains("_"))
                            {
                                aumids.Add(name);
                            }
                        }
                    }
                }

                // Another approach for modern apps - directly from package cache
                using (RegistryKey packagesKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\Repository\Packages"))
                {
                    if (packagesKey != null)
                    {
                        foreach (string packageName in packagesKey.GetSubKeyNames())
                        {
                            using (RegistryKey packageKey = packagesKey.OpenSubKey($"{packageName}\\App"))
                            {
                                if (packageKey != null)
                                {
                                    foreach (string appId in packageKey.GetSubKeyNames())
                                    {
                                        string aumid = $"{packageName}!{appId}";
                                        if (!aumids.Contains(aumid))
                                        {
                                            aumids.Add(aumid);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Remove duplicates
                aumids = aumids.Distinct().ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting installed application AUMIDs: {ex.Message}");
            }

            return aumids;
        }
    }
}
