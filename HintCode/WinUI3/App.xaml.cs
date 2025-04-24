using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using System;

namespace WinUI3
{
    public partial class App : Application
    {
        private MainWindow? m_window;
        private bool m_launchedViaProtocol = false;

        public App()
        {
            this.InitializeComponent();
            
            // Using Activated event for all activations (including launch)
            AppInstance.GetCurrent().Activated += OnActivated;
        }

        private void OnActivated(object? sender, AppActivationArguments args)
        {
            // Ensure execution is on the UI thread
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(() =>
            {
                 HandleActivation(args);
            });
        }

        private void HandleActivation(AppActivationArguments args)
        {
             ExtendedActivationKind kind = args.Kind;

            // Handle Protocol activation
            if (kind == ExtendedActivationKind.Protocol)
            {
                var protocolArgs = args.Data as ProtocolActivatedEventArgs;
                if (protocolArgs != null)
                {
                    m_launchedViaProtocol = true;
                    ProcessProtocolUri(protocolArgs.Uri);

                    if (m_window == null) // First launch via protocol
                    {
                        m_window = new MainWindow();
                        m_window.Initialize(startHidden: true); // Initialize hidden, start monitoring
                        // Do not call m_window.Activate() here
                    }
                    else // Already running, activated via protocol again
                    {
                        // Bring to front if minimized/hidden? What's desired?
                        // For now, let's just ensure it's shown (if it was hidden) and let AppWindow_Changed handle positioning if restored.
                         m_window._appWindow?.Show(); // Use AppWindow.Show()
                    }
                }
            }
            // Handle regular Launch activation (e.g., Start Menu, Tile)
            else if (kind == ExtendedActivationKind.Launch)
            {
                m_launchedViaProtocol = false; // Ensure flag is reset
                if (m_window == null) // First regular launch
                {
                    m_window = new MainWindow();
                    m_window.Initialize(startHidden: false); // Initialize visible, position TopLeft
                    // Initialize calls AppWindow.Show(), no need for Activate()?
                    // Let's call Activate() for standard launch to ensure focus etc.
                    m_window.Activate(); 
                }
                else // Already running, activated via Launch again (e.g., clicking tile)
                {
                     m_window.Activate(); // Bring the existing window to the foreground
                     // Positioning on restore is handled by AppWindow_Changed in MainWindow
                }
            }
            // Add handlers for other activation kinds if needed (File, Toast, etc.)
        }

        private void ProcessProtocolUri(Uri uri)
        {
            // Here you can handle different URI commands
            System.Diagnostics.Debug.WriteLine($"Protocol Activated with URI: {uri}");
            // Example: winui3app://command?param=value
            // Add your protocol handling logic here
        }

        // OnLaunched is not strictly needed if using AppInstance.Activated for everything
        // protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args) { }
    }
}