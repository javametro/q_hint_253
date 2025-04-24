using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBasedNotification
{
    // Event arguments containing the event ID
    public class NotificationEventArgs : EventArgs
    {
        public string EventId { get; }

        public NotificationEventArgs(string eventId)
        {
            EventId = eventId;
        }
    }

    // Interface for notification service
    public interface INotificationService
    {
        event EventHandler<NotificationEventArgs> NotificationReceived;
        void StartListening();
        void StopListening();
    }

    // Test implementation that fires dummy events
    public class DummyNotificationService : INotificationService
    {
        public event EventHandler<NotificationEventArgs> NotificationReceived;
        private System.Timers.Timer _timer;
        private int _counter = 0;
        private bool _isRunning = false;

        public DummyNotificationService()
        {
            _timer = new System.Timers.Timer(2000); // Fire event every 2 seconds
            _timer.Elapsed += (sender, e) => 
            {
                if (_isRunning)
                {
                    FireDummyEvent();
                }
            };
        }

        public void StartListening()
        {
            Console.WriteLine("Started listening for events...");
            _isRunning = true;
            _timer.Start();
        }

        public void StopListening()
        {
            Console.WriteLine("Stopped listening for events.");
            _isRunning = false;
            _timer.Stop();
        }

        private void FireDummyEvent()
        {
            _counter++;
            string eventId = $"EVENT_{_counter}_{Guid.NewGuid().ToString().Substring(0, 8)}";
            NotificationReceived?.Invoke(this, new NotificationEventArgs(eventId));
        }
    }

    // Real implementation would handle pipe communication
    // public class PipeNotificationService : INotificationService
    // {
    //     // Implementation for real pipe-based communication would go here
    // }

    internal class Program
    {
        static void Main(string[] args)
        {
            // Create notification service
            INotificationService notificationService = new DummyNotificationService();
            
            // Subscribe to the event
            notificationService.NotificationReceived += (sender, e) =>
            {
                Console.WriteLine($"Event received with ID: {e.EventId}");
            };

            // Start listening
            notificationService.StartListening();
            
            Console.WriteLine("Press any key to stop listening...");
            Console.ReadKey();
            
            // Stop listening
            notificationService.StopListening();
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
