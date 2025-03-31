using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StatusGetter
{
    /// <summary>
    /// Interface for accessing smartphone photo information
    /// </summary>
    public interface IPhotoInfo
    {
        /// <summary>
        /// Gets information about smartphone photos in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of smartphone photo information</returns>
        List<SmartphonePhotoInfo> GetSmartphonePhotos(string folderPath, bool includeSubfolders = true);

        /// <summary>
        /// Groups smartphone photos by device model
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>Dictionary mapping device models to lists of photos</returns>
        Dictionary<string, List<SmartphonePhotoInfo>> GetPhotosByDeviceModel(string folderPath, bool includeSubfolders = true);

        /// <summary>
        /// Gets smartphone photos taken within a specific date range
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="startDate">Start date of the range</param>
        /// <param name="endDate">End date of the range</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of smartphone photos taken within the date range</returns>
        List<SmartphonePhotoInfo> GetPhotosByDateRange(string folderPath, DateTime startDate, DateTime endDate, bool includeSubfolders = true);
    }

    /// <summary>
    /// Contains detailed information about a smartphone photo
    /// </summary>
    public class SmartphonePhotoInfo
    {
        /// <summary>
        /// Full path to the photo file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// File name of the photo
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Photo file size in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Date and time when the photo was taken
        /// </summary>
        public DateTime? DateTaken { get; set; }

        /// <summary>
        /// Manufacturer of the device that took the photo
        /// </summary>
        public string DeviceManufacturer { get; set; }

        /// <summary>
        /// Model of the device that took the photo
        /// </summary>
        public string DeviceModel { get; set; }

        /// <summary>
        /// Width of the photo in pixels
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Height of the photo in pixels
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// GPS latitude of where the photo was taken (if available)
        /// </summary>
        public double? GPSLatitude { get; set; }

        /// <summary>
        /// GPS longitude of where the photo was taken (if available)
        /// </summary>
        public double? GPSLongitude { get; set; }

        /// <summary>
        /// File extension of the photo
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        /// Exposure time used for the photo
        /// </summary>
        public string ExposureTime { get; set; }

        /// <summary>
        /// F-number (aperture) used for the photo
        /// </summary>
        public string FNumber { get; set; }

        /// <summary>
        /// ISO speed used for the photo
        /// </summary>
        public int? ISOSpeed { get; set; }

        /// <summary>
        /// Whether flash was used for the photo
        /// </summary>
        public bool? FlashUsed { get; set; }

        /// <summary>
        /// Focal length used for the photo
        /// </summary>
        public string FocalLength { get; set; }

        /// <summary>
        /// Formatted file size (e.g., "1.5 MB")
        /// </summary>
        public string FormattedFileSize => FormatSize(FileSizeBytes);

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

        /// <summary>
        /// Returns a string representation of the photo information
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"File: {FileName} ({FormattedFileSize})");
            sb.AppendLine($"Dimensions: {Width}x{Height}");

            if (DateTaken.HasValue)
                sb.AppendLine($"Date Taken: {DateTaken.Value}");

            if (!string.IsNullOrEmpty(DeviceManufacturer) || !string.IsNullOrEmpty(DeviceModel))
                sb.AppendLine($"Device: {DeviceManufacturer} {DeviceModel}".Trim());

            return sb.ToString();
        }
    }

    //指定フォルダのスマホ画像ファイル情報
    //指定フォルダ内（サブフォルダ含む）のスマホで撮影されたファイルの一覧情報を取得する。
    //ファイル一覧取得はDirectory.GetFilesなど使用。スマホで撮影された画像ファイルかどうかの判定にはExif情報を参照。
    public class PhotoInfo : IPhotoInfo
    {
        // Image file extensions to check
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".heic", ".heif" };

        /// <summary>
        /// Gets information about smartphone photos in the specified folder
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of smartphone photo information</returns>
        public List<SmartphonePhotoInfo> GetSmartphonePhotos(string folderPath, bool includeSubfolders = true)
        {
            List<SmartphonePhotoInfo> photoInfos = new List<SmartphonePhotoInfo>();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    return photoInfos;
                }

                SearchOption searchOption = includeSubfolders
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                foreach (string extension in _imageExtensions)
                {
                    string[] files = Directory.GetFiles(folderPath, $"*{extension}", searchOption);

                    foreach (string filePath in files)
                    {
                        try
                        {
                            SmartphonePhotoInfo photoInfo = ExtractPhotoInfo(filePath);
                            if (IsSmartphonePhoto(photoInfo))
                            {
                                photoInfos.Add(photoInfo);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing folder {folderPath}: {ex.Message}");
            }

            return photoInfos;
        }

        /// <summary>
        /// Groups smartphone photos by device model
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>Dictionary mapping device models to lists of photos</returns>
        public Dictionary<string, List<SmartphonePhotoInfo>> GetPhotosByDeviceModel(string folderPath, bool includeSubfolders = true)
        {
            var photosByModel = new Dictionary<string, List<SmartphonePhotoInfo>>();
            var photos = GetSmartphonePhotos(folderPath, includeSubfolders);

            foreach (var photo in photos)
            {
                string modelKey = !string.IsNullOrEmpty(photo.DeviceModel)
                    ? photo.DeviceModel
                    : "Unknown";

                if (!photosByModel.ContainsKey(modelKey))
                {
                    photosByModel[modelKey] = new List<SmartphonePhotoInfo>();
                }

                photosByModel[modelKey].Add(photo);
            }

            return photosByModel;
        }

        /// <summary>
        /// Gets smartphone photos taken within a specific date range
        /// </summary>
        /// <param name="folderPath">Path to the folder to search</param>
        /// <param name="startDate">Start date of the range</param>
        /// <param name="endDate">End date of the range</param>
        /// <param name="includeSubfolders">Whether to include subfolders in the search</param>
        /// <returns>List of smartphone photos taken within the date range</returns>
        public List<SmartphonePhotoInfo> GetPhotosByDateRange(string folderPath, DateTime startDate, DateTime endDate, bool includeSubfolders = true)
        {
            var photos = GetSmartphonePhotos(folderPath, includeSubfolders);

            return photos.Where(p =>
                p.DateTaken.HasValue &&
                p.DateTaken.Value >= startDate &&
                p.DateTaken.Value <= endDate).ToList();
        }

        /// <summary>
        /// Extracts photo information from a file
        /// </summary>
        private SmartphonePhotoInfo ExtractPhotoInfo(string filePath)
        {
            var photoInfo = new SmartphonePhotoInfo
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                FileExtension = Path.GetExtension(filePath).ToLower(),
                FileSizeBytes = new FileInfo(filePath).Length
            };

            try
            {
                using (Image image = Image.FromFile(filePath))
                {
                    photoInfo.Width = image.Width;
                    photoInfo.Height = image.Height;

                    // Extract EXIF information
                    PropertyItem[] propItems = image.PropertyItems;

                    // Extract device information
                    photoInfo.DeviceManufacturer = GetExifValue(propItems, 0x010F);  // Make
                    photoInfo.DeviceModel = GetExifValue(propItems, 0x0110);         // Model

                    // Extract date taken
                    string dateTakenStr = GetExifValue(propItems, 0x9003);           // DateTimeOriginal
                    if (!string.IsNullOrEmpty(dateTakenStr))
                    {
                        try
                        {
                            // EXIF format: "YYYY:MM:DD HH:MM:SS"
                            string[] parts = dateTakenStr.Split(' ');
                            string datePart = parts[0].Replace(":", "/");
                            photoInfo.DateTaken = DateTime.Parse($"{datePart} {parts[1]}");
                        }
                        catch
                        {
                            // Use file date if EXIF date parsing fails
                            photoInfo.DateTaken = File.GetCreationTime(filePath);
                        }
                    }
                    else
                    {
                        photoInfo.DateTaken = File.GetCreationTime(filePath);
                    }

                    // Extract GPS information
                    photoInfo.GPSLatitude = GetGpsCoordinate(propItems, 0x0002, 0x0001);  // GPSLatitude
                    photoInfo.GPSLongitude = GetGpsCoordinate(propItems, 0x0004, 0x0003); // GPSLongitude

                    // Extract camera settings
                    photoInfo.ExposureTime = GetExifValue(propItems, 0x829A);      // ExposureTime
                    photoInfo.FNumber = GetExifValue(propItems, 0x829D);           // FNumber

                    string isoStr = GetExifValue(propItems, 0x8827);               // ISO
                    if (!string.IsNullOrEmpty(isoStr) && int.TryParse(isoStr, out int iso))
                    {
                        photoInfo.ISOSpeed = iso;
                    }

                    photoInfo.FocalLength = GetExifValue(propItems, 0x920A);       // FocalLength

                    string flashStr = GetExifValue(propItems, 0x9209);             // Flash
                    if (!string.IsNullOrEmpty(flashStr) && int.TryParse(flashStr, out int flash))
                    {
                        photoInfo.FlashUsed = (flash & 0x1) == 1;
                    }
                }
            }
            catch
            {
                // Unable to extract EXIF data, continue with basic file info
            }

            return photoInfo;
        }

        /// <summary>
        /// Determines if a photo was taken by a smartphone based on its metadata
        /// </summary>
        private bool IsSmartphonePhoto(SmartphonePhotoInfo photoInfo)
        {
            // If we have a device model, check if it looks like a smartphone
            if (!string.IsNullOrEmpty(photoInfo.DeviceModel))
            {
                // Common smartphone manufacturers
                string[] smartphoneBrands = {
                    "Apple", "iPhone", "Samsung", "Huawei", "Xiaomi", "OPPO", "vivo",
                    "Motorola", "OnePlus", "Google", "Pixel", "Sony", "LG", "HTC",
                    "Nokia", "ASUS", "ZTE", "Realme", "Honor", "Redmi"
                };

                // Check if any smartphone brand is in the manufacturer or model
                string deviceInfo = $"{photoInfo.DeviceManufacturer} {photoInfo.DeviceModel}".ToLower();
                if (smartphoneBrands.Any(brand => deviceInfo.Contains(brand.ToLower())))
                {
                    return true;
                }

                // Check for model numbers that look like smartphone models
                if (Regex.IsMatch(photoInfo.DeviceModel, @"^(SM-[A-Z]\d|iPhone|Pixel|MI \d|Redmi)"))
                {
                    return true;
                }
            }

            // Check for GPS data, which is common in smartphone photos
            if (photoInfo.GPSLatitude.HasValue && photoInfo.GPSLongitude.HasValue)
            {
                return true;
            }

            // Check the aspect ratio - many smartphone photos are 4:3 or 16:9
            if (photoInfo.Width > 0 && photoInfo.Height > 0)
            {
                double ratio = (double)photoInfo.Width / photoInfo.Height;
                if (Math.Abs(ratio - 4.0 / 3.0) < 0.1 || Math.Abs(ratio - 16.0 / 9.0) < 0.1)
                {
                    // If the photo has a common smartphone resolution and some EXIF data
                    if (!string.IsNullOrEmpty(photoInfo.DeviceModel) ||
                        !string.IsNullOrEmpty(photoInfo.DeviceManufacturer) ||
                        photoInfo.DateTaken.HasValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Extracts a string value from EXIF property items
        /// </summary>
        private string GetExifValue(PropertyItem[] properties, int propId)
        {
            PropertyItem prop = properties.FirstOrDefault(p => p.Id == propId);
            if (prop != null)
            {
                // Most string values in EXIF are ASCII encoded and null-terminated
                if (prop.Type == 2) // ASCII string
                {
                    string value = Encoding.ASCII.GetString(prop.Value).TrimEnd('\0');
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
                else if (prop.Type == 3) // Short (16-bit unsigned int)
                {
                    if (prop.Value.Length >= 2)
                    {
                        return BitConverter.ToUInt16(prop.Value, 0).ToString();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Extracts a GPS coordinate from EXIF property items
        /// </summary>
        private double? GetGpsCoordinate(PropertyItem[] properties, int coordinateId, int refId)
        {
            // Get the GPS coordinate values (degrees, minutes, seconds)
            PropertyItem coordProp = properties.FirstOrDefault(p => p.Id == coordinateId);
            PropertyItem refProp = properties.FirstOrDefault(p => p.Id == refId);

            if (coordProp != null && refProp != null && coordProp.Value.Length >= 24)
            {
                try
                {
                    // GPS coordinates are stored as three rational values (degrees, minutes, seconds)
                    // Each rational is two 4-byte values (numerator and denominator)

                    uint degreesNumerator = BitConverter.ToUInt32(coordProp.Value, 0);
                    uint degreesDenominator = BitConverter.ToUInt32(coordProp.Value, 4);
                    double degrees = degreesNumerator / (double)degreesDenominator;

                    uint minutesNumerator = BitConverter.ToUInt32(coordProp.Value, 8);
                    uint minutesDenominator = BitConverter.ToUInt32(coordProp.Value, 12);
                    double minutes = minutesNumerator / (double)minutesDenominator;

                    uint secondsNumerator = BitConverter.ToUInt32(coordProp.Value, 16);
                    uint secondsDenominator = BitConverter.ToUInt32(coordProp.Value, 20);
                    double seconds = secondsNumerator / (double)secondsDenominator;

                    double coordinate = degrees + minutes / 60 + seconds / 3600;

                    // Apply the reference direction (N/S or E/W)
                    char direction = (char)refProp.Value[0];
                    if (direction == 'S' || direction == 'W')
                    {
                        coordinate = -coordinate;
                    }

                    return coordinate;
                }
                catch
                {
                    // If parsing fails, return null
                    return null;
                }
            }

            return null;
        }
    }
}
