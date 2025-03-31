using StatusGetter.StatusGetter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusGetter
{
    //壁紙パス情報
    //設定中の壁紙パスを取得する。

//    ・SystemParametersInfoなど使用する。
//・ユーザが壁紙設定を変更しているかどうかを調べるために使用するので、背景の設定が「スライドショー」、「スポットライト」の場合はそのファイル名を削除したパス、「単色」の場合は空文字列、「画像」の場合はフルパス、としてください。
//スライドショー：C:\Users\[ユーザ名]\AppData\Roaming\Microsoft\Windows\Themes
    using Microsoft.Win32;
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    namespace StatusGetter
    {
        /// <summary>
        /// Interface for accessing wallpaper information
        /// </summary>
        public interface IWallPaperInfo
        {
            /// <summary>
            /// Gets the path to the current desktop wallpaper
            /// </summary>
            /// <returns>Path to the wallpaper image file</returns>
            string GetWallpaperPath();

            /// <summary>
            /// Gets the current wallpaper style (centered, stretched, etc.)
            /// </summary>
            /// <returns>WallpaperStyle enum value</returns>
            WallpaperStyle GetWallpaperStyle();

            /// <summary>
            /// Gets detailed information about the current wallpaper
            /// </summary>
            /// <returns>WallpaperDetails object with wallpaper information</returns>
            WallpaperDetails GetWallpaperDetails();

            /// <summary>
            /// Checks if the wallpaper is set to a default Windows wallpaper
            /// </summary>
            /// <returns>True if using a default wallpaper, false otherwise</returns>
            bool IsDefaultWallpaper();
        }

        /// <summary>
        /// Enum representing wallpaper display styles
        /// </summary>
        public enum WallpaperStyle
        {
            /// <summary>Wallpaper is centered on screen</summary>
            Centered = 0,

            /// <summary>Wallpaper is tiled to fill screen</summary>
            Tiled = 1,

            /// <summary>Wallpaper is stretched to fill screen</summary>
            Stretched = 2,

            /// <summary>Wallpaper is fitted to screen</summary>
            Fit = 6,

            /// <summary>Wallpaper fills screen (crop if needed)</summary>
            Fill = 10,

            /// <summary>Wallpaper is displayed across multiple monitors</summary>
            Span = 22,

            /// <summary>Unknown style</summary>
            Unknown = -1
        }

        /// <summary>
        /// Contains detailed information about the wallpaper
        /// </summary>
        public class WallpaperDetails
        {
            /// <summary>
            /// Path to the wallpaper image file
            /// </summary>
            public string WallpaperPath { get; set; }

            /// <summary>
            /// Wallpaper display style
            /// </summary>
            public WallpaperStyle Style { get; set; }

            /// <summary>
            /// Whether the wallpaper is tiled
            /// </summary>
            public bool IsTiled { get; set; }

            /// <summary>
            /// Whether the current wallpaper is a default Windows wallpaper
            /// </summary>
            public bool IsDefaultWallpaper { get; set; }

            /// <summary>
            /// Resolution of the wallpaper image (if available)
            /// </summary>
            public Size? ImageResolution { get; set; }

            /// <summary>
            /// File size of the wallpaper image in bytes (if available)
            /// </summary>
            public long? FileSizeBytes { get; set; }

            /// <summary>
            /// File name of the wallpaper image
            /// </summary>
            public string FileName => Path.GetFileName(WallpaperPath);

            /// <summary>
            /// File extension of the wallpaper image
            /// </summary>
            public string FileExtension => Path.GetExtension(WallpaperPath);

            /// <summary>
            /// Formatted file size (e.g., "1.5 MB") if available
            /// </summary>
            public string FormattedFileSize => FileSizeBytes.HasValue ? FormatSize(FileSizeBytes.Value) : "Unknown";

            /// <summary>
            /// String representation of the wallpaper style
            /// </summary>
            public string StyleDescription
            {
                get
                {
                    switch (Style)
                    {
                        case WallpaperStyle.Centered: return "Centered";
                        case WallpaperStyle.Tiled: return "Tiled";
                        case WallpaperStyle.Stretched: return "Stretched";
                        case WallpaperStyle.Fit: return "Fit";
                        case WallpaperStyle.Fill: return "Fill";
                        case WallpaperStyle.Span: return "Span";
                        default: return "Unknown";
                    }
                }
            }

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

        /// <summary>
        /// Provides information about the system's wallpaper settings
        /// </summary>
        public class WallPaperInfo : IWallPaperInfo
        {
            // P/Invoke declarations for accessing Windows API
            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            private static extern int SystemParametersInfo(int uAction, int uParam, StringBuilder lpvParam, int fuWinIni);

            private const int SPI_GETDESKWALLPAPER = 0x0073;
            private const int MAX_PATH = 260;

            // Registry keys for accessing wallpaper settings
            private const string DesktopRegistryPath = @"Control Panel\Desktop";
            private const string DefaultWallpaperFolder = @"C:\Windows\Web\Wallpaper\";

            /// <summary>
            /// Gets the path to the current desktop wallpaper
            /// </summary>
            /// <returns>Path to the wallpaper image file</returns>
            public string GetWallpaperPath()
            {
                StringBuilder wallpaperPath = new StringBuilder(MAX_PATH);
                SystemParametersInfo(SPI_GETDESKWALLPAPER, wallpaperPath.Capacity, wallpaperPath, 0);
                return wallpaperPath.ToString();
            }

            /// <summary>
            /// Gets the current wallpaper style (centered, stretched, etc.)
            /// </summary>
            /// <returns>WallpaperStyle enum value</returns>
            public WallpaperStyle GetWallpaperStyle()
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(DesktopRegistryPath))
                    {
                        if (key == null)
                            return WallpaperStyle.Unknown;

                        string wallpaperStyle = key.GetValue("WallpaperStyle") as string;
                        string tileWallpaper = key.GetValue("TileWallpaper") as string;

                        if (string.IsNullOrEmpty(wallpaperStyle))
                            return WallpaperStyle.Unknown;

                        // Handle the case where the wallpaper is tiled
                        if (tileWallpaper == "1")
                            return WallpaperStyle.Tiled;

                        // Parse the wallpaper style
                        if (int.TryParse(wallpaperStyle, out int style))
                        {
                            switch (style)
                            {
                                case 0: return WallpaperStyle.Centered;
                                case 2: return WallpaperStyle.Stretched;
                                case 6: return WallpaperStyle.Fit;
                                case 10: return WallpaperStyle.Fill;
                                case 22: return WallpaperStyle.Span;
                                default: return WallpaperStyle.Unknown;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting wallpaper style: {ex.Message}");
                }

                return WallpaperStyle.Unknown;
            }

            /// <summary>
            /// Gets detailed information about the current wallpaper
            /// </summary>
            /// <returns>WallpaperDetails object with wallpaper information</returns>
            public WallpaperDetails GetWallpaperDetails()
            {
                string wallpaperPath = GetWallpaperPath();
                WallpaperStyle style = GetWallpaperStyle();

                WallpaperDetails details = new WallpaperDetails
                {
                    WallpaperPath = wallpaperPath,
                    Style = style,
                    IsDefaultWallpaper = IsDefaultWallpaper(),
                    IsTiled = style == WallpaperStyle.Tiled
                };

                try
                {
                    if (File.Exists(wallpaperPath))
                    {
                        // Get file size
                        FileInfo fileInfo = new FileInfo(wallpaperPath);
                        details.FileSizeBytes = fileInfo.Length;

                        // Get image resolution
                        using (Image img = Image.FromFile(wallpaperPath))
                        {
                            details.ImageResolution = new Size(img.Width, img.Height);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting wallpaper details: {ex.Message}");
                }

                return details;
            }

            /// <summary>
            /// Checks if the wallpaper is set to a default Windows wallpaper
            /// </summary>
            /// <returns>True if using a default wallpaper, false otherwise</returns>
            public bool IsDefaultWallpaper()
            {
                string wallpaperPath = GetWallpaperPath();

                // Check if the wallpaper is in the default Windows wallpaper folder
                return !string.IsNullOrEmpty(wallpaperPath) &&
                       wallpaperPath.StartsWith(DefaultWallpaperFolder, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
//スポットライト：C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\
//DesktopSpotlight\Assets\Images\

    internal class WallPaperInfo
    {
    }
}


//// Example: Get basic wallpaper path
//IWallPaperInfo wallpaperInfo = new WallPaperInfo();
//string wallpaperPath = wallpaperInfo.GetWallpaperPath();
//Console.WriteLine($"Current wallpaper: {wallpaperPath}");

//// Example: Get wallpaper style
//WallpaperStyle style = wallpaperInfo.GetWallpaperStyle();
//Console.WriteLine($"Wallpaper style: {style}");

//// Example: Get detailed wallpaper information
//WallpaperDetails details = wallpaperInfo.GetWallpaperDetails();
//Console.WriteLine($"Wallpaper: {details.FileName}");
//Console.WriteLine($"Style: {details.StyleDescription}");
//if (details.ImageResolution.HasValue)
//{
//    Console.WriteLine($"Resolution: {details.ImageResolution.Value.Width}x{details.ImageResolution.Value.Height}");
//}
//Console.WriteLine($"File size: {details.FormattedFileSize}");
//Console.WriteLine($"Default wallpaper: {details.IsDefaultWallpaper}");

