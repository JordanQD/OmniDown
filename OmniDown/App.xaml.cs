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
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace OmniDown
{
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

        private sealed record PendingActivation(
            string? DownloadText,
            TaskNotificationInvokedEventArgs? Notification);

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            PendingActivation activation = GetActivation(args.Arguments);
            if (!TryAcquireSingleInstance())
            {
                SignalExistingInstance(activation);
                Exit();
                return;
            }

            CreateAndActivateMainWindow(activation);
        }

        private static PendingActivation GetActivation(string launchArguments)
        {
            try
            {
                AppActivationArguments activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();
                if (activatedEventArgs.Kind == ExtendedActivationKind.Protocol &&
                    activatedEventArgs.Data is ProtocolActivatedEventArgs protocolArgs)
                {
                    string? protocolText = protocolArgs.Uri?.ToString();
                    if (SystemNotificationService.TryParseActivationArguments(protocolText, out TaskNotificationInvokedEventArgs protocolNotificationArgs))
                    {
                        return new PendingActivation(null, protocolNotificationArgs);
                    }

                    return new PendingActivation(protocolText, null);
                }
            }
            catch
            {
            }

            string? activationText = NormalizeActivationText(launchArguments);
            if (SystemNotificationService.TryParseActivationArguments(activationText, out TaskNotificationInvokedEventArgs parsedNotificationArgs))
            {
                return new PendingActivation(null, parsedNotificationArgs);
            }

            return new PendingActivation(activationText, null);
        }

        private static string? NormalizeActivationText(string activationText)
        {
            if (string.IsNullOrWhiteSpace(activationText))
            {
                return null;
            }

            return activationText.Trim().Trim('"');
        }

        private void CreateAndActivateMainWindow(PendingActivation activation)
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            StartActivationListener();
            _window = new MainWindow();
            _window.Closed += MainWindow_Closed;
            Notifications.Register();
            bool shouldActivate = activation.Notification is null ||
                !SystemNotificationService.IsOpenDownloadedAction(activation.Notification.Action);
            if (shouldActivate)
            {
                _window.Activate();
            }

            if (_window is MainWindow mainWindow)
            {
                if (activation.Notification is not null)
                {
                    mainWindow.HandleNotificationActivation(activation.Notification);
                }
                else if (!string.IsNullOrWhiteSpace(activation.DownloadText))
                {
                    _ = mainWindow.HandleExternalDownloadTextAsync(activation.DownloadText);
                }
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

        private static void SignalExistingInstance(PendingActivation activation)
        {
            try
            {
                string? pendingActivationText = SerializePendingActivation(activation);
                if (!string.IsNullOrWhiteSpace(pendingActivationText))
                {
                    Directory.CreateDirectory(AppPaths.LocalDataDirectory);
                    File.WriteAllText(PendingActivationPath, pendingActivationText);
                }

                using EventWaitHandle activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
                activationEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }
        }

        private static string? SerializePendingActivation(PendingActivation activation)
        {
            if (activation.Notification is not null)
            {
                return SystemNotificationService.CreateActivationArguments(
                    activation.Notification.Action,
                    activation.Notification.Gid,
                    activation.Notification.FilePath,
                    activation.Notification.FolderPath);
            }

            return NormalizeActivationText(activation.DownloadText ?? string.Empty);
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
                if (TryReadPendingActivation(out PendingActivation activation))
                {
                    if (activation.Notification is not null)
                    {
                        mainWindow.HandleNotificationActivation(activation.Notification);
                    }
                    else
                    {
                        mainWindow.ShowAndActivate();
                        if (!string.IsNullOrWhiteSpace(activation.DownloadText))
                        {
                            _ = mainWindow.HandleExternalDownloadTextAsync(activation.DownloadText);
                        }
                    }
                }
                else
                {
                    mainWindow.ShowAndActivate();
                }

                return;
            }

            _window?.Activate();
        }

        private static bool TryReadPendingActivation(out PendingActivation activation)
        {
            activation = new PendingActivation(null, null);
            try
            {
                if (!File.Exists(PendingActivationPath))
                {
                    return false;
                }

                string activationText = File.ReadAllText(PendingActivationPath);
                File.Delete(PendingActivationPath);
                if (string.IsNullOrWhiteSpace(activationText))
                {
                    return false;
                }

                if (SystemNotificationService.TryParseActivationArguments(activationText, out TaskNotificationInvokedEventArgs notificationArgs))
                {
                    activation = new PendingActivation(null, notificationArgs);
                    return true;
                }

                activation = new PendingActivation(activationText, null);
                return true;
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
