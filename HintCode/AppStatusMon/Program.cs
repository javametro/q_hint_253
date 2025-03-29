using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppStatusMon
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ApplicationEventWatcher watcher = new ApplicationEventWatcher();
            watcher.StartWatching();

            Console.WriteLine("Application is running. Press Ctrl+C to exit...");

            // Keep the application running indefinitely
            while (true)
            {
                // Optionally, you can add a small delay to reduce CPU usage
                System.Threading.Thread.Sleep(1000);
            }

            // watcher.StopWatching(); // This line will never be reached
        }
    }
}
