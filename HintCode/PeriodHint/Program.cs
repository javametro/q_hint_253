using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PeriodHint
{
    public interface INotificationItem
    {
        DateTime CreatedDate { get; }
        bool IsRead { get; }
        string Content { get; }
        void MarkAsRead();
    }

    public class NotificationItem : INotificationItem
    {
        public DateTime CreatedDate { get; private set; }
        public bool IsRead { get; private set; }
        public string Content { get; private set; }

        public NotificationItem(string content)
        {
            Content = content;
            CreatedDate = DateTime.Now;
            IsRead = false;
        }

        public void MarkAsRead()
        {
            IsRead = true;
        }
    }

    public interface INotificationService
    {
        IReadOnlyList<INotificationItem> GetNotifications();
        void AddNotification(string content);
        void MarkAsRead(int index);
        void CheckAndAddNotification(bool shouldAddNotification, string content);
    }

    public class NotificationService : INotificationService
    {
        private List<INotificationItem> _notifications = new List<INotificationItem>();
        private const int MaxCapacity = 3;

        public IReadOnlyList<INotificationItem> GetNotifications()
        {
            return _notifications.AsReadOnly();
        }

        public void AddNotification(string content)
        {
            var notification = new NotificationItem(content);
            
            if (_notifications.Count < MaxCapacity)
            {
                _notifications.Add(notification);
            }
            else
            {
                // Check if there are any read items to remove
                var readItems = _notifications.Where(n => n.IsRead).ToList();
                if (readItems.Any())
                {
                    // Remove all read items
                    foreach (var item in readItems)
                    {
                        _notifications.Remove(item);
                    }
                    _notifications.Add(notification);
                }
                else
                {
                    // Replace the oldest item
                    _notifications.RemoveAt(0);
                    _notifications.Add(notification);
                }
            }
        }

        public void MarkAsRead(int index)
        {
            if (index >= 0 && index < _notifications.Count)
            {
                _notifications[index].MarkAsRead();
            }
        }

        public void CheckAndAddNotification(bool shouldAddNotification, string content)
        {
            if (shouldAddNotification)
            {
                AddNotification(content);
            }
        }
    }

    public interface IApplicationActivityChecker
    {
        bool HasNotOpenedAppForPeriod(TimeSpan period);
    }

    public class NotePadActivityChecker : IApplicationActivityChecker
    {
        private const string ApplicationName = "notepad.exe";
        private DateTime? _lastCheckTime = null;
        
        public bool HasNotOpenedAppForPeriod(TimeSpan period)
        {
            DateTime now = DateTime.Now;
            bool result = false;

            if (_lastCheckTime == null || now - _lastCheckTime.Value >= period)
            {
                // Check if notepad.exe has been run in the last period
                result = !HasRunNotepadRecently(period);
                _lastCheckTime = now;
            }

            return result;
        }

        private bool HasRunNotepadRecently(TimeSpan period)
        {
            // This is a simplified implementation
            // In a real implementation, you might check the system logs or other sources
            // to determine if notepad.exe has been run
            
            // For demonstration purposes, we'll check if notepad is currently running
            Process[] processes = Process.GetProcessesByName("notepad");
            return processes.Length > 0;

            // Note: A real implementation would need to track when notepad was last opened
            // which might require hooking into system events or regularly checking
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Create services
            INotificationService notificationService = new NotificationService();
            IApplicationActivityChecker activityChecker = new NotePadActivityChecker();
            
            // For testing purposes, using 10 seconds instead of two weeks
            TimeSpan testPeriod = TimeSpan.FromSeconds(10);
            
            // For demonstration, we'll simulate several checks with console input
            Console.WriteLine("Notification System Demo");
            Console.WriteLine("------------------------");
            
            while (true)
            {
                // Display current notifications
                DisplayNotifications(notificationService);
                
                Console.WriteLine("\nOptions:");
                Console.WriteLine("1. Simulate check for new notification");
                Console.WriteLine("2. Mark notification as read");
                Console.WriteLine("3. Exit");
                Console.Write("Choose an option: ");
                
                string choice = Console.ReadLine();
                
                switch (choice)
                {
                    case "1":
                        // Simulate a check
                        bool shouldAddNotification = activityChecker.HasNotOpenedAppForPeriod(testPeriod);
                        notificationService.CheckAndAddNotification(
                            shouldAddNotification, 
                            $"You haven't opened Notepad for 10 seconds: {DateTime.Now}");
                        
                        Console.WriteLine(shouldAddNotification ? 
                            "A new notification was added!" : 
                            "No notification needed at this time.");
                        break;
                    
                    case "2":
                        Console.Write("Enter the notification index to mark as read: ");
                        if (int.TryParse(Console.ReadLine(), out int index))
                        {
                            notificationService.MarkAsRead(index);
                            Console.WriteLine("Notification marked as read.");
                        }
                        else
                        {
                            Console.WriteLine("Invalid index.");
                        }
                        break;
                    
                    case "3":
                        return;
                    
                    default:
                        Console.WriteLine("Invalid choice. Try again.");
                        break;
                }
                
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
                Console.Clear();
            }
        }
        
        static void DisplayNotifications(INotificationService notificationService)
        {
            var notifications = notificationService.GetNotifications();
            
            Console.WriteLine($"\nCurrent Notifications ({notifications.Count}):");
            if (notifications.Count == 0)
            {
                Console.WriteLine("No notifications.");
                return;
            }
            
            for (int i = 0; i < notifications.Count; i++)
            {
                var item = notifications[i];
                Console.WriteLine($"[{i}] {item.Content} - Created: {item.CreatedDate} - {(item.IsRead ? "READ" : "UNREAD")}");
            }
        }
    }
}
