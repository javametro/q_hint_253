using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace AppStatusMon
{
    public class ApplicationEventWatcher
    {
        private ManagementEventWatcher startWatcher;
        private ManagementEventWatcher stopWatcher;

        public void StartWatching()
        {
            // Query for application start events
            string startQuery = "SELECT * FROM Win32_ProcessStartTrace";
            startWatcher = new ManagementEventWatcher(new WqlEventQuery(startQuery));
            startWatcher.EventArrived += new EventArrivedEventHandler(OnProcessStart);
            startWatcher.Start();

            // Query for application stop events
            string stopQuery = "SELECT * FROM Win32_ProcessStopTrace";
            stopWatcher = new ManagementEventWatcher(new WqlEventQuery(stopQuery));
            stopWatcher.EventArrived += new EventArrivedEventHandler(OnProcessStop);
            stopWatcher.Start();
        }

        private void OnProcessStart(object sender, EventArrivedEventArgs e)
        {
            Console.WriteLine("Process started: " + e.NewEvent["ProcessName"]);
        }

        private void OnProcessStop(object sender, EventArrivedEventArgs e)
        {
            Console.WriteLine("Process stopped: " + e.NewEvent["ProcessName"]);
        }

        public void StopWatching()
        {
            if (startWatcher != null)
            {
                startWatcher.Stop();
                startWatcher.Dispose();
            }

            if (stopWatcher != null)
            {
                stopWatcher.Stop();
                stopWatcher.Dispose();
            }
        }
    }
}
