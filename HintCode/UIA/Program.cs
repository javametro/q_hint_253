using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UIA
{
    internal class Program
    {
        static void Main(string[] args)
        {
            StartAndAlignApplications();
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private static void StartAndAlignApplications()
        {
            // Start Notepad
            Process notepad = Process.Start("notepad.exe");
            notepad.WaitForInputIdle();

            // Start MSInfo32
            Process msinfo32 = Process.Start("msinfo32.exe");
            msinfo32.WaitForInputIdle();

            // Find the windows
            IntPtr notepadHandle = FindWindow(null, "Untitled - Notepad");
            IntPtr msinfo32Handle = FindWindow(null, "System Information");

            if (notepadHandle == IntPtr.Zero || msinfo32Handle == IntPtr.Zero)
            {
                Console.WriteLine("Could not find one or both windows.");
                return;
            }

            // Get screen dimensions
            int screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

            // Calculate window dimensions
            int windowWidth = screenWidth / 2;
            int windowHeight = screenHeight;

            // Align Notepad to the left
            SetWindowPos(notepadHandle, IntPtr.Zero, 0, 0, windowWidth, windowHeight, SWP_NOZORDER | SWP_SHOWWINDOW);

            // Align MSInfo32 to the right
            SetWindowPos(msinfo32Handle, IntPtr.Zero, windowWidth, 0, windowWidth, windowHeight, SWP_NOZORDER | SWP_SHOWWINDOW);
        }
    }
}
