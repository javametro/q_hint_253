using Microsoft.Win32;
using StatusGetter;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing mouse settings information
    /// </summary>
    public interface IMouseInfo
    {
        /// <summary>
        /// Gets comprehensive information about mouse settings
        /// </summary>
        /// <returns>MouseSettings object containing all mouse settings</returns>
        MouseSettings GetMouseSettings();

        /// <summary>
        /// Gets the current mouse position
        /// </summary>
        /// <returns>Point representing the current mouse position</returns>
        Point GetMousePosition();

        /// <summary>
        /// Gets the current mouse cursor size
        /// </summary>
        /// <returns>Size representing the cursor dimensions</returns>
        int GetMouseCursorSize();

        /// <summary>
        /// Gets the current mouse cursor scheme
        /// </summary>
        /// <returns>String representing the cursor scheme name</returns>
        string GetMouseCursorScheme();

        /// <summary>
        /// Gets the mouse speed setting
        /// </summary>
        /// <returns>Integer representing the mouse speed (1-20)</returns>
        int GetMouseSpeed();

        /// <summary>
        /// Gets the mouse accessibility settings from the registry
        /// </summary>
        /// <returns>MouseAccessibilitySettings object containing accessibility settings</returns>
        MouseAccessibilitySettings GetMouseAccessibilitySettings();

        /// <summary>
        /// Checks if mouse trails are enabled
        /// </summary>
        /// <returns>True if mouse trails are enabled, false otherwise</returns>
        bool AreMouseTrailsEnabled();

        /// <summary>
        /// Gets the number of connected mouse devices
        /// </summary>
        /// <returns>Integer representing the number of connected mouse devices</returns>
        int GetConnectedMouseCount();
    }

    /// <summary>
    /// Contains detailed information about mouse settings
    /// </summary>
    public class MouseSettings
    {
        /// <summary>
        /// Mouse cursor size (pixels or percentage)
        /// </summary>
        public int CursorSize { get; set; }

        /// <summary>
        /// Mouse cursor scheme name
        /// </summary>
        public string CursorScheme { get; set; }

        /// <summary>
        /// Mouse speed setting (1-20)
        /// </summary>
        public int MouseSpeed { get; set; }

        /// <summary>
        /// Mouse sensitivity
        /// </summary>
        public int MouseSensitivity { get; set; }

        /// <summary>
        /// Whether mouse acceleration is enabled
        /// </summary>
        public bool AccelerationEnabled { get; set; }

        /// <summary>
        /// Whether primary and secondary mouse buttons are swapped
        /// </summary>
        public bool SwapMouseButtons { get; set; }

        /// <summary>
        /// Double-click time in milliseconds
        /// </summary>
        public int DoubleClickTime { get; set; }

        /// <summary>
        /// Double-click width in pixels
        /// </summary>
        public int DoubleClickWidth { get; set; }

        /// <summary>
        /// Double-click height in pixels
        /// </summary>
        public int DoubleClickHeight { get; set; }

        /// <summary>
        /// Whether mouse trails are enabled
        /// </summary>
        public bool MouseTrailsEnabled { get; set; }

        /// <summary>
        /// Mouse trails length (if enabled)
        /// </summary>
        public int MouseTrailsLength { get; set; }

        /// <summary>
        /// Number of connected mouse devices
        /// </summary>
        public int ConnectedMouseCount { get; set; }

        /// <summary>
        /// Mouse accessibility settings
        /// </summary>
        public MouseAccessibilitySettings AccessibilitySettings { get; set; }

        /// <summary>
        /// Whether the mouse is a touchpad
        /// </summary>
        public bool IsTouchpad { get; set; }

        /// <summary>
        /// Type of mouse (if detectable)
        /// </summary>
        public string MouseType { get; set; }

        /// <summary>
        /// Mouse wheel present
        /// </summary>
        public bool HasWheel { get; set; }

        /// <summary>
        /// Number of buttons on the mouse
        /// </summary>
        public int ButtonCount { get; set; }

        /// <summary>
        /// Whether scroll inversion is enabled
        /// </summary>
        public bool ScrollInverted { get; set; }

        /// <summary>
        /// Vertical scroll lines per notch
        /// </summary>
        public int ScrollLinesPerNotch { get; set; }

        /// <summary>
        /// Whether the "Show location of pointer when the CTRL key is pressed" option is enabled
        /// </summary>
        public bool ShowPointerOnCtrlPress { get; set; }

        /// <summary>
        /// Size of the locator circle when CTRL is pressed (if ShowPointerOnCtrlPress is enabled)
        /// </summary>
        public int PointerLocatorSize { get; set; }

        /// <summary>
        /// Returns a string representation of the mouse settings
        /// </summary>
        public override string ToString()
        {
            return $"Cursor Size: {CursorSize}, Scheme: {CursorScheme}, " +
                   $"Speed: {MouseSpeed}, Buttons Swapped: {SwapMouseButtons}, " +
                   $"Double-Click Time: {DoubleClickTime}ms, Trails: {MouseTrailsEnabled}, " +
                   $"Show Pointer on CTRL: {ShowPointerOnCtrlPress}";
        }
    }

    /// <summary>
    /// Contains mouse accessibility settings
    /// </summary>
    public class MouseAccessibilitySettings
    {
        /// <summary>
        /// Whether MouseKeys is enabled (control pointer with numeric keypad)
        /// </summary>
        public bool MouseKeysEnabled { get; set; }

        /// <summary>
        /// MouseKeys maximum speed (pixels per second)
        /// </summary>
        public int MouseKeysMaxSpeed { get; set; }

        /// <summary>
        /// MouseKeys acceleration
        /// </summary>
        public int MouseKeysAcceleration { get; set; }

        /// <summary>
        /// Whether ClickLock is enabled (hold down mouse button without holding)
        /// </summary>
        public bool ClickLockEnabled { get; set; }

        /// <summary>
        /// ClickLock time (milliseconds to hold down mouse button to lock it)
        /// </summary>
        public int ClickLockTime { get; set; }

        /// <summary>
        /// Whether a visual indicator is shown when the mouse button is clicked
        /// </summary>
        public bool MouseSonarEnabled { get; set; }

        /// <summary>
        /// Whether mouse cursor is highlighted when Ctrl is pressed
        /// </summary>
        public bool MouseVanishEnabled { get; set; }

        /// <summary>
        /// Whether cursor shadow is enabled
        /// </summary>
        public bool CursorShadowEnabled { get; set; }

        /// <summary>
        /// Returns a string representation of the mouse accessibility settings
        /// </summary>
        public override string ToString()
        {
            return $"MouseKeys: {MouseKeysEnabled}, ClickLock: {ClickLockEnabled}, " +
                   $"MouseSonar: {MouseSonarEnabled}, CursorShadow: {CursorShadowEnabled}";
        }
    }

    /// <summary>
    /// Provides information about mouse settings
    /// </summary>
    public class MouseInfo : IMouseInfo
    {
        #region Native Methods and Structures

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uAction, int uParam, ref int lpvParam, int fuWinIni);

        [DllImport("user32.dll")]
        private static extern bool SystemParametersInfo(int uAction, int uParam, ref bool lpvParam, int fuWinIni);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetSystemMetrics(int nIndex);

        // System parameters actions
        private const int SPI_GETMOUSESPEED = 0x0070;
        private const int SPI_GETMOUSE = 0x0003;
        private const int SPI_GETMOUSETRAILS = 0x005E;
        private const int SPI_GETDOUBLECLICKTIME = 0x0028;
        private const int SPI_GETDOUBLECLICKTIMEHEIGHT = 0x0030;
        private const int SPI_GETDOUBLECLICKTIMEWIDTH = 0x0029;
        private const int SPI_GETWHEELSCROLLLINES = 0x0068;
        private const int SPI_GETMOUSEHOVERTIME = 0x0066;
        private const int SPI_GETMOUSEHOVERHEIGHT = 0x0064;
        private const int SPI_GETMOUSEHOVERWIDTH = 0x0062;
        private const int SPI_GETMOUSEVANISH = 0x1020;
        private const int SPI_GETMOUSESONAR = 0x101C;

        // GetSystemMetrics indices
        private const int SM_SWAPBUTTON = 23;
        private const int SM_MOUSEPRESENT = 19;
        private const int SM_MOUSEWHEELPRESENT = 75;
        private const int SM_CMOUSEBUTTONS = 43;

        // Registry paths
        private const string AccessibilityRegistryKey = @"Software\Microsoft\Accessibility";
        private const string MouseRegistryKey = @"Control Panel\Mouse";
        private const string CursorsRegistryKey = @"Control Panel\Cursors";
        private const string CurrentThemeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

        #endregion

        /// <summary>
        /// Gets comprehensive information about mouse settings
        /// </summary>
        /// <returns>MouseSettings object containing all mouse settings</returns>
        public MouseSettings GetMouseSettings()
        {
            MouseSettings settings = new MouseSettings
            {
                CursorSize = GetMouseCursorSize(),
                CursorScheme = GetMouseCursorScheme(),
                MouseSpeed = GetMouseSpeed(),
                SwapMouseButtons = GetSystemMetrics(SM_SWAPBUTTON) != 0,
                DoubleClickTime = System.Windows.Forms.SystemInformation.DoubleClickTime,
                DoubleClickWidth = System.Windows.Forms.SystemInformation.DoubleClickSize.Width,
                DoubleClickHeight = System.Windows.Forms.SystemInformation.DoubleClickSize.Height,
                MouseTrailsEnabled = AreMouseTrailsEnabled(),
                MouseTrailsLength = GetMouseTrailsLength(),
                ConnectedMouseCount = GetConnectedMouseCount(),
                AccessibilitySettings = GetMouseAccessibilitySettings(),
                HasWheel = GetSystemMetrics(SM_MOUSEWHEELPRESENT) != 0,
                ButtonCount = GetSystemMetrics(SM_CMOUSEBUTTONS),
                ScrollLinesPerNotch = System.Windows.Forms.SystemInformation.MouseWheelScrollLines
            };

            // Get mouse sensitivity and acceleration
            int[] mouseParams = new int[3];
            bool accelerationEnabled = false;
            GetMouseParams(out mouseParams[0], out mouseParams[1], out mouseParams[2], out accelerationEnabled);
            settings.MouseSensitivity = mouseParams[0];
            settings.AccelerationEnabled = accelerationEnabled;

            // Try to determine if it's a touchpad
            try
            {
                settings.IsTouchpad = IsLikelyTouchpad();
                settings.MouseType = DetermineMouseType();
                settings.ScrollInverted = IsScrollInverted();
            }
            catch
            {
                // Not critical if these fail
            }

            // Get "Show pointer location when CTRL is pressed" setting
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MouseRegistryKey))
            {
                if (key != null)
                {
                    // Check for "MouseSonar" setting (show pointer location when CTRL is pressed)
                    object mouseSonar = key.GetValue("MouseSonar");
                    if (mouseSonar != null && int.TryParse(mouseSonar.ToString(), out int sonarEnabled))
                    {
                        settings.ShowPointerOnCtrlPress = sonarEnabled != 0;
                    }

                    // Get the size of the pointer locator circle (if available)
                    object pointerLocatorRadius = key.GetValue("SonarRadius");
                    if (pointerLocatorRadius != null && int.TryParse(pointerLocatorRadius.ToString(), out int radius))
                    {
                        settings.PointerLocatorSize = radius;
                    }
                    else
                    {
                        // Default size is typically 20
                        settings.PointerLocatorSize = 20;
                    }
                }
            }


            return settings;
        }

        /// <summary>
        /// Gets the current mouse position
        /// </summary>
        /// <returns>Point representing the current mouse position</returns>
        public Point GetMousePosition()
        {
            return System.Windows.Forms.Cursor.Position;
        }

        /// <summary>
        /// Gets the current mouse cursor size
        /// </summary>
        /// <returns>Size representing the cursor dimensions</returns>
        public int GetMouseCursorSize()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AccessibilityRegistryKey))
            {
                if (key != null)
                {
                    object value = key.GetValue("CursorSize");
                    if (value != null && int.TryParse(value.ToString(), out int cursorSize))
                    {
                        return cursorSize;
                    }
                }
            }

            // Default size or fallback to Windows default (which is typically 1, representing 100%)
            return 1;
        }

        /// <summary>
        /// Gets the current mouse cursor scheme
        /// </summary>
        /// <returns>String representing the cursor scheme name</returns>
        public string GetMouseCursorScheme()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(CursorsRegistryKey))
            {
                if (key != null)
                {
                    object value = key.GetValue("Scheme");
                    if (value != null)
                    {
                        return value.ToString();
                    }
                }
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets the mouse speed setting
        /// </summary>
        /// <returns>Integer representing the mouse speed (1-20)</returns>
        public int GetMouseSpeed()
        {
            int speed = 10; // Default value
            SystemParametersInfo(SPI_GETMOUSESPEED, 0, ref speed, 0);
            return speed;
        }

        /// <summary>
        /// Gets the mouse accessibility settings from the registry
        /// </summary>
        /// <returns>MouseAccessibilitySettings object containing accessibility settings</returns>
        public MouseAccessibilitySettings GetMouseAccessibilitySettings()
        {
            MouseAccessibilitySettings settings = new MouseAccessibilitySettings();

            // Get settings from Accessibility registry key
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AccessibilityRegistryKey))
            {
                if (key != null)
                {
                    // MouseKeys settings
                    object mouseKeysOn = key.GetValue("MouseKeysOn");
                    if (mouseKeysOn != null && int.TryParse(mouseKeysOn.ToString(), out int mkOn))
                    {
                        settings.MouseKeysEnabled = mkOn != 0;
                    }

                    object mouseKeysMaxSpeed = key.GetValue("MouseKeysMaxSpeed");
                    if (mouseKeysMaxSpeed != null && int.TryParse(mouseKeysMaxSpeed.ToString(), out int mkSpeed))
                    {
                        settings.MouseKeysMaxSpeed = mkSpeed;
                    }

                    object mouseKeysAccel = key.GetValue("MouseKeysAccel");
                    if (mouseKeysAccel != null && int.TryParse(mouseKeysAccel.ToString(), out int mkAccel))
                    {
                        settings.MouseKeysAcceleration = mkAccel;
                    }

                    // ClickLock settings
                    settings.ClickLockEnabled = IsClickLockEnabled();
                    settings.ClickLockTime = GetClickLockTime();
                }
            }

            // Get additional settings from other sources
            bool sonarEnabled = false;
            SystemParametersInfo(SPI_GETMOUSESONAR, 0, ref sonarEnabled, 0);
            settings.MouseSonarEnabled = sonarEnabled;

            bool vanishEnabled = false;
            SystemParametersInfo(SPI_GETMOUSEVANISH, 0, ref vanishEnabled, 0);
            settings.MouseVanishEnabled = vanishEnabled;

            // Check cursor shadow from Mouse registry key
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MouseRegistryKey))
            {
                if (key != null)
                {
                    object enableCursorShadow = key.GetValue("EnableCursorShadow");
                    if (enableCursorShadow != null && int.TryParse(enableCursorShadow.ToString(), out int shadowEnabled))
                    {
                        settings.CursorShadowEnabled = shadowEnabled != 0;
                    }
                }
            }

            return settings;
        }

        /// <summary>
        /// Checks if mouse trails are enabled
        /// </summary>
        /// <returns>True if mouse trails are enabled, false otherwise</returns>
        public bool AreMouseTrailsEnabled()
        {
            int trailLength = 0;
            SystemParametersInfo(SPI_GETMOUSETRAILS, 0, ref trailLength, 0);
            return trailLength > 1;
        }

        /// <summary>
        /// Gets the number of connected mouse devices
        /// </summary>
        /// <returns>Integer representing the number of connected mouse devices</returns>
        public int GetConnectedMouseCount()
        {
            return GetSystemMetrics(SM_MOUSEPRESENT) != 0 ? 1 : 0;
        }

        #region Helper Methods

        /// <summary>
        /// Gets the mouse trails length
        /// </summary>
        private int GetMouseTrailsLength()
        {
            int trailLength = 0;
            SystemParametersInfo(SPI_GETMOUSETRAILS, 0, ref trailLength, 0);
            return trailLength;
        }

        /// <summary>
        /// Gets the mouse sensitivity parameters
        /// </summary>
        private void GetMouseParams(out int threshold1, out int threshold2, out int speedFactor, out bool enhancePointerPrecision)
        {
            int[] mouseParams = new int[3];
            SystemParametersInfo(SPI_GETMOUSE, 0, ref mouseParams[0], 0);

            threshold1 = mouseParams[0];
            threshold2 = mouseParams[1];
            speedFactor = mouseParams[2];

            // Check if enhance pointer precision is enabled
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MouseRegistryKey))
            {
                if (key != null)
                {
                    object epp = key.GetValue("MouseSpeed");
                    enhancePointerPrecision = epp != null && epp.ToString() == "1";
                }
                else
                {
                    enhancePointerPrecision = false;
                }
            }
        }

        /// <summary>
        /// Checks if ClickLock is enabled
        /// </summary>
        private bool IsClickLockEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MouseRegistryKey))
            {
                if (key != null)
                {
                    object clickLock = key.GetValue("ClickLock");
                    return clickLock != null && clickLock.ToString() == "1";
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the ClickLock time (milliseconds)
        /// </summary>
        private int GetClickLockTime()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MouseRegistryKey))
            {
                if (key != null)
                {
                    object clickLockTime = key.GetValue("ClickLockTime");
                    if (clickLockTime != null && int.TryParse(clickLockTime.ToString(), out int time))
                    {
                        return time;
                    }
                }
            }
            return 1200; // Default value
        }

        /// <summary>
        /// Attempts to determine if the pointing device is likely a touchpad
        /// </summary>
        private bool IsLikelyTouchpad()
        {
            // Check for common touchpad registry indicators
            string[] touchpadDeviceKeys = {
                @"SYSTEM\CurrentControlSet\Services\SynTP",
                @"SYSTEM\CurrentControlSet\Services\ETD",
                @"SYSTEM\CurrentControlSet\Services\ASUS Smart Gesture",
                @"SYSTEM\CurrentControlSet\Services\AlpsAlpine"
            };

            foreach (string keyPath in touchpadDeviceKeys)
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath))
                {
                    if (key != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Attempts to determine the type of mouse (wired, wireless, touchpad, etc.)
        /// </summary>
        private string DetermineMouseType()
        {
            if (IsLikelyTouchpad())
            {
                return "Touchpad";
            }

            // Very basic check - could be expanded with more registry checks
            bool mousePresent = GetSystemMetrics(SM_MOUSEPRESENT) != 0;
            if (!mousePresent)
            {
                return "None";
            }

            // Check for Bluetooth (very simplistic approach)
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTHUSB"))
            {
                if (key != null)
                {
                    return "Wireless (possibly Bluetooth)";
                }
            }

            return "Standard Mouse";
        }

        /// <summary>
        /// Checks if natural scrolling (inverted scroll) is enabled
        /// </summary>
        private bool IsScrollInverted()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\HID"))
            {
                if (key != null)
                {
                    // This is a very simplistic check and may not work on all systems
                    foreach (string subKeyName in key.GetSubKeyNames())
                    {
                        using (RegistryKey deviceKey = key.OpenSubKey(subKeyName))
                        {
                            if (deviceKey != null)
                            {
                                foreach (string deviceSubKey in deviceKey.GetSubKeyNames())
                                {
                                    using (RegistryKey deviceInstance = deviceKey.OpenSubKey(deviceSubKey + @"\Device Parameters"))
                                    {
                                        if (deviceInstance != null)
                                        {
                                            object flipped = deviceInstance.GetValue("FlipFlopWheel");
                                            if (flipped != null && flipped.ToString() == "1")
                                            {
                                                return true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        #endregion
    }
}


//// Example: Get all mouse settings
//IMouseInfo mouseInfo = new MouseInfo();
//MouseSettings settings = mouseInfo.GetMouseSettings();

//Console.WriteLine($"Cursor Size: {settings.CursorSize}");
//Console.WriteLine($"Cursor Scheme: {settings.CursorScheme}");
//Console.WriteLine($"Mouse Speed: {settings.MouseSpeed}");
//Console.WriteLine($"Buttons Swapped: {settings.SwapMouseButtons}");
//Console.WriteLine($"Double-Click Time: {settings.DoubleClickTime} ms");
//Console.WriteLine($"Mouse Trails Enabled: {settings.MouseTrailsEnabled}");
//Console.WriteLine($"Device Type: {settings.MouseType}");
//Console.WriteLine($"Has Wheel: {settings.HasWheel}");
//Console.WriteLine($"Button Count: {settings.ButtonCount}");

//// Example: Get current mouse position
//Point position = mouseInfo.GetMousePosition();
//Console.WriteLine($"Current Mouse Position: ({position.X}, {position.Y})");

//// Example: Get mouse accessibility settings
//MouseAccessibilitySettings accessibility = mouseInfo.GetMouseAccessibilitySettings();
//Console.WriteLine($"MouseKeys Enabled: {accessibility.MouseKeysEnabled}");
//Console.WriteLine($"ClickLock Enabled: {accessibility.ClickLockEnabled}");
//Console.WriteLine($"Mouse Sonar: {accessibility.MouseSonarEnabled}");
//Console.WriteLine($"Cursor Shadow: {accessibility.CursorShadowEnabled}");




