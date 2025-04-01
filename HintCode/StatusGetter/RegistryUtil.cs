using Microsoft.Win32;
using StatusGetter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;

//指定レジストリのキー値の取得
//指定したレジストリのキー値（種類、値）を取得する。
//レジストリ情報を取得する。


namespace StatusGetter
{
    /// <summary>
    /// Interface for registry utility operations
    /// </summary>
    public interface IRegistryUtil
    {
        /// <summary>
        /// Gets a registry value
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value to retrieve</param>
        /// <returns>The registry value or null if not found</returns>
        object GetValue(RegistryHive hive, string keyPath, string valueName);

        /// <summary>
        /// Gets a registry value with a default value if not found
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value to retrieve</param>
        /// <param name="defaultValue">Default value to return if the registry value is not found</param>
        /// <returns>The registry value or the default value</returns>
        object GetValue(RegistryHive hive, string keyPath, string valueName, object defaultValue);

        /// <summary>
        /// Gets a registry value as a specific type
        /// </summary>
        /// <typeparam name="T">Type to convert the registry value to</typeparam>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value to retrieve</param>
        /// <param name="defaultValue">Default value to return if the registry value is not found or cannot be converted</param>
        /// <returns>The registry value converted to the specified type, or the default value</returns>
        T GetValue<T>(RegistryHive hive, string keyPath, string valueName, T defaultValue);

        /// <summary>
        /// Gets all values in a registry key
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <returns>Dictionary of value names and values, or empty dictionary if key not found</returns>
        Dictionary<string, object> GetValues(RegistryHive hive, string keyPath);

        /// <summary>
        /// Gets the registry value kind (type)
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value</param>
        /// <returns>Registry value kind, or null if the value is not found</returns>
        RegistryValueKind? GetValueKind(RegistryHive hive, string keyPath, string valueName);

        /// <summary>
        /// Checks if a registry key exists
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <returns>True if the key exists, false otherwise</returns>
        bool KeyExists(RegistryHive hive, string keyPath);

        /// <summary>
        /// Checks if a registry value exists
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value</param>
        /// <returns>True if the value exists, false otherwise</returns>
        bool ValueExists(RegistryHive hive, string keyPath, string valueName);

        /// <summary>
        /// Gets all subkeys of a registry key
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <returns>Array of subkey names, or empty array if key not found</returns>
        string[] GetSubKeyNames(RegistryHive hive, string keyPath);
    }

    /// <summary>
    /// Utility class for registry operations
    /// </summary>
    public class RegistryUtil : IRegistryUtil
    {
        /// <summary>
        /// Opens a registry key with the specified access rights
        /// </summary>
        /// <param name="hive">Registry hive</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="writable">Whether to open the key with write access</param>
        /// <returns>RegistryKey object or null if not found</returns>
        private RegistryKey OpenKey(RegistryHive hive, string keyPath, bool writable = false)
        {
            try
            {
                RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                return baseKey?.OpenSubKey(keyPath, writable);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening registry key {hive}\\{keyPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets a registry value
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value to retrieve</param>
        /// <returns>The registry value or null if not found</returns>
        public object GetValue(RegistryHive hive, string keyPath, string valueName)
        {
            using (RegistryKey key = OpenKey(hive, keyPath))
            {
                return key?.GetValue(valueName);
            }
        }

        /// <summary>
        /// Gets a registry value with a default value if not found
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value to retrieve</param>
        /// <param name="defaultValue">Default value to return if the registry value is not found</param>
        /// <returns>The registry value or the default value</returns>
        public object GetValue(RegistryHive hive, string keyPath, string valueName, object defaultValue)
        {
            using (RegistryKey key = OpenKey(hive, keyPath))
            {
                return key?.GetValue(valueName, defaultValue);
            }
        }

        /// <summary>
        /// Gets a registry value as a specific type
        /// </summary>
        /// <typeparam name="T">Type to convert the registry value to</typeparam>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value to retrieve</param>
        /// <param name="defaultValue">Default value to return if the registry value is not found or cannot be converted</param>
        /// <returns>The registry value converted to the specified type, or the default value</returns>
        public T GetValue<T>(RegistryHive hive, string keyPath, string valueName, T defaultValue)
        {
            object value = GetValue(hive, keyPath, valueName);

            if (value == null)
                return defaultValue;

            try
            {
                if (typeof(T).IsEnum && value is int)
                {
                    // Handle enum conversion
                    return (T)Enum.ToObject(typeof(T), value);
                }

                // Handle standard type conversion
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting registry value: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Gets all values in a registry key
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <returns>Dictionary of value names and values, or empty dictionary if key not found</returns>
        public Dictionary<string, object> GetValues(RegistryHive hive, string keyPath)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();

            using (RegistryKey key = OpenKey(hive, keyPath))
            {
                if (key == null)
                    return values;

                foreach (string valueName in key.GetValueNames())
                {
                    try
                    {
                        values[valueName] = key.GetValue(valueName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting registry value {valueName}: {ex.Message}");
                    }
                }
            }

            return values;
        }

        /// <summary>
        /// Gets the registry value kind (type)
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value</param>
        /// <returns>Registry value kind, or null if the value is not found</returns>
        public RegistryValueKind? GetValueKind(RegistryHive hive, string keyPath, string valueName)
        {
            using (RegistryKey key = OpenKey(hive, keyPath))
            {
                if (key == null)
                    return null;

                try
                {
                    return key.GetValueKind(valueName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting registry value kind: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Checks if a registry key exists
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public bool KeyExists(RegistryHive hive, string keyPath)
        {
            using (RegistryKey key = OpenKey(hive, keyPath))
            {
                return key != null;
            }
        }

        /// <summary>
        /// Checks if a registry value exists
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <param name="valueName">Name of the value</param>
        /// <returns>True if the value exists, false otherwise</returns>
        public bool ValueExists(RegistryHive hive, string keyPath, string valueName)
        {
            using (RegistryKey key = OpenKey(hive, keyPath))
            {
                if (key == null)
                    return false;

                return key.GetValueNames().Contains(valueName);
            }
        }

        /// <summary>
        /// Gets all subkeys of a registry key
        /// </summary>
        /// <param name="hive">Registry hive (e.g., HKEY_LOCAL_MACHINE)</param>
        /// <param name="keyPath">Path to the registry key</param>
        /// <returns>Array of subkey names, or empty array if key not found</returns>
        public string[] GetSubKeyNames(RegistryHive hive, string keyPath)
        {
            using (RegistryKey key = OpenKey(hive, keyPath))
            {
                return key?.GetSubKeyNames() ?? new string[0];
            }
        }
    }
}


//// Example: Get a registry value (string)
//IRegistryUtil registryUtil = new RegistryUtil();
//string productName = registryUtil.GetValue<string>(
//    RegistryHive.LocalMachine,
//    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
//    "ProductName",
//    "Unknown Windows Version");

//Console.WriteLine($"Windows Version: {productName}");

//// Example: Check if a registry key exists
//bool keyExists = registryUtil.KeyExists(
//    RegistryHive.CurrentUser,
//    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");

//Console.WriteLine($"Key exists: {keyExists}");

//// Example: Get all values in a registry key
//Dictionary<string, object> values = registryUtil.GetValues(
//    RegistryHive.LocalMachine,
//    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");

//foreach (var pair in values)
//{
//    Console.WriteLine($"{pair.Key} = {pair.Value}");
//}

//// Example: Get registry value type
//RegistryValueKind? valueKind = registryUtil.GetValueKind(
//    RegistryHive.LocalMachine,
//    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
//    "ProductName");

//if (valueKind.HasValue)
//{
//    Console.WriteLine($"Value type: {valueKind.Value}");
//}

//// Example: Get integer value with default
//int dwordValue = registryUtil.GetValue<int>(
//    RegistryHive.CurrentUser,
//    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced",
//    "HideFileExt",
//    1);

//Console.WriteLine($"Hide File Extensions: {dwordValue}");

//// Example: Get subkeys
//string[] subKeys = registryUtil.GetSubKeyNames(
//    RegistryHive.LocalMachine,
//    @"SOFTWARE\Microsoft");

//Console.WriteLine("Microsoft software:");
//foreach (string subKey in subKeys)
//{
//    Console.WriteLine($"- {subKey}");
//}
