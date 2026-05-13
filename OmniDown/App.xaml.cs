using Microsoft.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.AppLifecycle;
using OmniDown.Services.Notifications;
using OmniDown.Services.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OmniDown
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private const string SingleInstanceMutexName = @"Local\OmniDown.SingleInstance";
        private const string ActivationEventName = @"Local\OmniDown.ActivateExistingInstance";
        private static readonly string PendingActivationPath = System.IO.Path.Combine(AppPaths.LocalDataDirectory, "pending-activation.txt");

        private Window? _window;
        private DispatcherQueue? _dispatcherQueue;
        private Mutex? _singleInstanceMutex;
        private EventWaitHandle? _activationEvent;
        private CancellationTokenSource? _activationListenerCancellation;
        private Task? _activationListenerTask;

        public SystemNotificationService Notifications { get; } = new();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            string? activationText = GetActivationText(args.Arguments);
            if (!TryAcquireSingleInstance())
            {
                SignalExistingInstance(activationText);
                Exit();
                return;
            }

            CreateAndActivateMainWindow(activationText);
        }

        private static string? GetActivationText(string launchArguments)
        {
            try
            {
                AppActivationArguments activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                if (activatedEventArgs.Kind == ExtendedActivationKind.Protocol &&
                    activatedEventArgs.Data is ProtocolActivatedEventArgs protocolArgs)
                {
                    return protocolArgs.Uri?.ToString();
                }
            }
            catch
            {
            }

            return string.IsNullOrWhiteSpace(launchArguments) ? null : launchArguments;
        }

        private void CreateAndActivateMainWindow(string? activationText)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            StartActivationListener();
            _window = new MainWindow();
            _window.Closed += MainWindow_Closed;
            Notifications.Register();
            _window.Activate();
            if (_window is MainWindow mainWindow && !string.IsNullOrWhiteSpace(activationText))
            {
                _ = mainWindow.HandleExternalDownloadTextAsync(activationText);
            }
        }

        private bool TryAcquireSingleInstance()
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out bool createdNew);
            if (!createdNew)
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                return false;
            }

            _activationEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
            return true;
        }

        private static void SignalExistingInstance(string? activationText)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(activationText))
                {
                    Directory.CreateDirectory(AppPaths.LocalDataDirectory);
                    File.WriteAllText(PendingActivationPath, activationText);
                }

                using EventWaitHandle activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
                activationEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }
        }

        private void StartActivationListener()
        {
            if (_activationEvent is null)
            {
                return;
            }

            _activationListenerCancellation = new CancellationTokenSource();
            CancellationToken token = _activationListenerCancellation.Token;
            _activationListenerTask = Task.Run(() =>
            {
                WaitHandle[] waitHandles = [_activationEvent, token.WaitHandle];
                while (!token.IsCancellationRequested)
                {
                    int signaledIndex = WaitHandle.WaitAny(waitHandles);
                    if (signaledIndex == 0)
                    {
                        _dispatcherQueue?.TryEnqueue(ActivateExistingWindow);
                    }
                }
            }, token);
        }

        private void ActivateExistingWindow()
        {
            if (_window is MainWindow mainWindow)
            {
                mainWindow.ShowAndActivate();
                if (TryReadPendingActivation(out string activationText))
                {
                    _ = mainWindow.HandleExternalDownloadTextAsync(activationText);
                }

                return;
            }

            _window?.Activate();
        }

        private static bool TryReadPendingActivation(out string activationText)
        {
            activationText = string.Empty;
            try
            {
                if (!File.Exists(PendingActivationPath))
                {
                    return false;
                }

                activationText = File.ReadAllText(PendingActivationPath);
                File.Delete(PendingActivationPath);
                return !string.IsNullOrWhiteSpace(activationText);
            }
            catch
            {
                return false;
            }
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            StopActivationListener();
        }

        private void StopActivationListener()
        {
            _activationListenerCancellation?.Cancel();
            _activationListenerCancellation?.Dispose();
            _activationListenerCancellation = null;
            _activationListenerTask = null;
            _activationEvent?.Dispose();
            _activationEvent = null;
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            _singleInstanceMutex = null;
        }
    }
}
