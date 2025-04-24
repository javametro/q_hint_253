using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using System.Runtime.InteropServices; // Needed for P/Invoke
using WinRT.Interop; // Needed for window interop
using Microsoft.UI.Dispatching; // Needed for DispatcherQueue
using System.Diagnostics; // Needed for Debug.WriteLine

namespace WinUI3
{
    // Enum to define window positions
    public enum WindowPositionType
    {
        TopLeft,
        BottomRight,
        Center
    }

    public sealed partial class MainWindow : Window
    {
        // Win32 API Constants
        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;

        // P/Invoke Signatures
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool IntersectRect(out RECT lprcDst, [In] ref RECT lprcSrc1, [In] ref RECT lprcSrc2);


        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        // Member variables
        internal AppWindow _appWindow;
        private DisplayArea _displayArea;
        private IntPtr _hookHandle = IntPtr.Zero;
        private WinEventDelegate _winEventDelegate; // Keep delegate alive
        private OverlappedPresenterState _previousState = OverlappedPresenterState.Restored; // Track previous state

        private bool _isInitiallyHidden = false;
        private bool _wasMinimizedByOverlap = false;
        private const int AppWidth = 800;
        private const int AppHeight = 600;


        public ObservableCollection<ListItem> ListItems { get; } = new ObservableCollection<ListItem>();

        public MainWindow()
        {
            this.InitializeComponent();
            Title = "WinUI 3 Horizontal List Demo";

            // Get AppWindow and DisplayArea
            var hWnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);
            _displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

            // Set initial size
            _appWindow.Resize(new SizeInt32(AppWidth, AppHeight));

            // Register for state changes and closing event
            _appWindow.Changed += AppWindow_Changed;
            this.Closed += MainWindow_Closed; // Hook Closed event
             _winEventDelegate = new WinEventDelegate(WinEventProc); // Initialize delegate instance

            // Add sample items (Keep existing item adding logic)
            ListItems.Add(new ListItem
            {
                Title = "Mountain View",
                Description = "A beautiful mountain landscape with snow peaks and clear blue sky.",
                ImagePath = "ms-appx:///Assets/Mountain.jpg",
                IconGlyph = "\uE774"
            }); // Mountain icon

            ListItems.Add(new ListItem
            {
                Title = "Beach Paradise",
                Description = "Tropical beach with palm trees and crystal clear water.",
                ImagePath = "ms-appx:///Assets/Beach.jpg",
                IconGlyph = "\uE706"
            }); // Beach icon

            ListItems.Add(new ListItem
            {
                Title = "Urban Landscape",
                Description = "Modern cityscape with skyscrapers and busy streets.",
                ImagePath = "ms-appx:///Assets/City.jpg",
                IconGlyph = "\uEC02"
            }); // City icon

            ListItems.Add(new ListItem
            {
                Title = "Forest Retreat",
                Description = "Dense forest with tall trees and a path leading through.",
                ImagePath = "ms-appx:///Assets/Forest.jpg",
                IconGlyph = "\uEA86"
            }); // Tree icon

            ListItems.Add(new ListItem
            {
                Title = "Desert Adventure",
                Description = "Vast desert landscape with sand dunes stretching to the horizon.",
                ImagePath = "ms-appx:///Assets/Desert.jpg",
                IconGlyph = "\uE753"
            }); // Sun icon

            ListItems.Add(new ListItem
            {
                Title = "Island Getaway",
                Description = "Secluded island with lush vegetation surrounded by blue ocean.",
                ImagePath = "ms-appx:///Assets/Island.jpg",
                IconGlyph = "\uE909"
            }); // Island icon

            // Set the items source
            HorizontalListView.ItemsSource = ListItems;
        }

        // Initialize is called from App.xaml.cs
        public void Initialize(bool startHidden)
        {
            Debug.WriteLine($"MainWindow Initialize called. startHidden: {startHidden}");
             if (_displayArea == null) // Ensure DisplayArea is valid
             {
                 var hWnd = WindowNative.GetWindowHandle(this);
                 var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
                 _displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
                 Debug.WriteLine($"DisplayArea initialized: {_displayArea?.WorkArea.Width}x{_displayArea?.WorkArea.Height}");
             }

            if (startHidden)
            {
                _isInitiallyHidden = true;
                Debug.WriteLine("Starting hidden, initiating desktop monitoring.");
                // Don't show, start monitoring
                StartDesktopMonitoring();
            }
            else
            {
                _isInitiallyHidden = false;
                Debug.WriteLine("Starting visible, positioning TopLeft and showing.");
                // Position top-left and show
                PositionWindow(WindowPositionType.TopLeft);
                 // Show needs to be done via AppWindow to work reliably before Activate
                 if (_appWindow != null)
                 {
                    _appWindow.Show(true); // Request activation when showing
                 }
            }
        }


        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
             if (args.DidPresenterChange && sender.Presenter is OverlappedPresenter presenter)
             {
                 var currentState = presenter.State;
                 Debug.WriteLine($"AppWindow State Changed: From {_previousState} to {currentState}"); // Debug
                 // Check if restored from minimized state
                 if (currentState == OverlappedPresenterState.Restored && _previousState == OverlappedPresenterState.Minimized)
                 {
                      Debug.WriteLine("Restored from Minimized. Positioning BottomRight."); // Debug
                     // Position bottom-right on restore from minimize
                     PositionWindow(WindowPositionType.BottomRight);
                     _wasMinimizedByOverlap = false; // Reset flag on any restore
                 }
                 _previousState = currentState; // Update previous state
             }
        }

        private void PositionWindow(WindowPositionType type)
        {
            if (_displayArea == null) return; // Guard against null display area

            var workArea = _displayArea.WorkArea;
            var windowSize = _appWindow.Size; // Use current AppWindow size

            int x = 0;
            int y = 0;

            switch (type)
            {
                case WindowPositionType.TopLeft:
                    x = workArea.X;
                    y = workArea.Y;
                    break;
                case WindowPositionType.BottomRight:
                    x = workArea.X + workArea.Width - windowSize.Width;
                    y = workArea.Y + workArea.Height - windowSize.Height;
                    break;
                case WindowPositionType.Center:
                    x = workArea.X + (workArea.Width - windowSize.Width) / 2;
                    y = workArea.Y + (workArea.Height - windowSize.Height) / 2;
                    break;
            }

            _appWindow.Move(new PointInt32(x, y));
        }


        // --- Desktop Monitoring Logic ---

        private void StartDesktopMonitoring()
        {
            if (_hookHandle == IntPtr.Zero)
            {
                 Debug.WriteLine("Attempting to set WinEventHook...");
                _hookHandle = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
                 Debug.WriteLine($"SetWinEventHook result (Hook Handle): {_hookHandle}");
                 if (_hookHandle == IntPtr.Zero)
                 {
                     // Get error code if hook failed
                     int errorCode = Marshal.GetLastWin32Error();
                     Debug.WriteLine($"Failed to set WinEventHook. Error code: {errorCode}");
                 }
            }
            else
            {
                 Debug.WriteLine("WinEventHook already set.");
            }
        }

        private void StopDesktopMonitoring()
        {
             Debug.WriteLine("Attempting to stop desktop monitoring...");
            if (_hookHandle != IntPtr.Zero)
            {
                 bool success = UnhookWinEvent(_hookHandle);
                 Debug.WriteLine($"UnhookWinEvent result: {success}");
                _hookHandle = IntPtr.Zero;
            }
            else
            {
                Debug.WriteLine("No active hook to unhook.");
            }
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            Debug.WriteLine($"WinEventProc fired. Event: {eventType}, HWND: {hwnd}");

            // Only process if initially hidden and not yet shown/minimized due to overlap
            if (!_isInitiallyHidden || _wasMinimizedByOverlap)
            {
                Debug.WriteLine($"WinEventProc: Skipping (isInitiallyHidden={_isInitiallyHidden}, wasMinimizedByOverlap={_wasMinimizedByOverlap})");
                return;
            }

             IntPtr foregroundHwnd = GetForegroundWindow();
             Debug.WriteLine($"Foreground Window HWND: {foregroundHwnd}");

             // Don't react to self
            if (foregroundHwnd == IntPtr.Zero || foregroundHwnd == WindowNative.GetWindowHandle(this))
            {
                 Debug.WriteLine("WinEventProc: Skipping (Foreground is Zero or Self)");
                 return;
            }

            if (GetWindowRect(foregroundHwnd, out RECT foregroundRect))
            {
                 Debug.WriteLine($"Foreground Window Rect: L={foregroundRect.Left}, T={foregroundRect.Top}, R={foregroundRect.Right}, B={foregroundRect.Bottom}");

                // Calculate the intended center position rect of our app window
                 if (_displayArea == null) {
                      Debug.WriteLine("WinEventProc: Skipping (DisplayArea is null)");
                      return;
                 }
                 var workArea = _displayArea.WorkArea;
                 RECT centeredAppRect = new RECT
                 {
                     Left = workArea.X + (workArea.Width - AppWidth) / 2,
                     Top = workArea.Y + (workArea.Height - AppHeight) / 2,
                     Right = workArea.X + (workArea.Width - AppWidth) / 2 + AppWidth,
                     Bottom = workArea.Y + (workArea.Height - AppHeight) / 2 + AppHeight
                 };
                 Debug.WriteLine($"Centered App Rect Target: L={centeredAppRect.Left}, T={centeredAppRect.Top}, R={centeredAppRect.Right}, B={centeredAppRect.Bottom}");

                // Check if the foreground window intersects with our centered position
                if (IntersectRect(out RECT intersection, ref centeredAppRect, ref foregroundRect))
                {
                     Debug.WriteLine($"Overlap detected! Intersection: L={intersection.Left}, T={intersection.Top}, R={intersection.Right}, B={intersection.Bottom}");

                     DispatcherQueue.TryEnqueue(() =>
                     {
                         Debug.WriteLine("Executing Show/Minimize on UI thread...");
                         if (!_wasMinimizedByOverlap && _isInitiallyHidden) // Double check flags inside UI thread
                         {
                             Debug.WriteLine("Show/Minimize conditions met on UI thread. Executing...");
                             _appWindow.Show();
                             if (_appWindow.Presenter is OverlappedPresenter presenter)
                             {
                                 presenter.Minimize();
                                 _previousState = OverlappedPresenterState.Minimized; // Manually set state after minimize
                             }
                             _wasMinimizedByOverlap = true;
                             _isInitiallyHidden = false; // No longer considered initially hidden
                             StopDesktopMonitoring(); // Stop monitoring once triggered
                             Debug.WriteLine("Show/Minimize executed. Monitoring stopped.");
                         }
                         else
                         {
                            Debug.WriteLine($"Show/Minimize skipped on UI thread. (wasMinimizedByOverlap={_wasMinimizedByOverlap}, isInitiallyHidden={_isInitiallyHidden})");
                         }
                     });
                }
                else
                {
                    Debug.WriteLine("No overlap detected.");
                }
            }
            else
            {
                 int errorCode = Marshal.GetLastWin32Error();
                 Debug.WriteLine($"GetWindowRect failed for HWND {foregroundHwnd}. Error code: {errorCode}");
            }
        }


        // --- Original ListView Logic ---
        private void HorizontalListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HorizontalListView.SelectedItem is ListItem selectedItem)
            {
                // You can handle item selection here
                // For example, show details in another pane or navigate to a details page
                System.Diagnostics.Debug.WriteLine($"Selected: {selectedItem.Title}");

                // Reset selection (optional)
                // HorizontalListView.SelectedItem = null;
            }
        }

        // Ensure monitoring stops when window closes
        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            StopDesktopMonitoring();
            // Unregister events if necessary
             _appWindow.Changed -= AppWindow_Changed;
             this.Closed -= MainWindow_Closed;
        }
    }

    // Keep existing ListItem class
    public class ListItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }
        public string IconGlyph { get; set; }
    }
}

// --- Cleanup ---
// Ensure StopDesktopMonitoring is called when the window closes
// Might need to add destructor or handle Closed event
// public MainWindow() { ... this.Closed += MainWindow_Closed; }
// private void MainWindow_Closed(object sender, WindowEventArgs args) { StopDesktopMonitoring(); }
// Need to add using Microsoft.UI.Dispatching; for DispatcherQueue
// Need to add using System.Diagnostics; for Debug.WriteLine