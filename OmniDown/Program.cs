using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using OmniDown.Services.Widgets;
using System;
using System.Threading;

namespace OmniDown;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // If launched by COM for widget activation, run as COM server (no UI)
        string cmdLine = Environment.CommandLine;
        if (cmdLine.Contains("-widgetserver", StringComparison.OrdinalIgnoreCase))
        {
            RunWidgetComServer();
            return;
        }

        // Normal launch: register COM factory, then start WinUI app
        int cookie = RegisterWidgetComFactory();
        try
        {
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }
        finally
        {
            if (cookie != 0)
            {
                Ole32.CoRevokeClassObject(cookie);
            }
        }
    }

    private static int RegisterWidgetComFactory()
    {
        var factory = new WidgetProviderClassFactory();
        Guid clsid = typeof(OmniDownWidgetProvider).GUID;

        int hr = Ole32.CoRegisterClassObject(
            clsid,
            factory,
            Ole32.CLSCTX_LOCAL_SERVER,
            Ole32.REGCLS_MULTIPLEUSE,
            out int cookie);

        if (hr >= 0)
        {
            Ole32.CoResumeClassObjects();
            return cookie;
        }

        return 0;
    }

    private static void RunWidgetComServer()
    {
        // COM already initialized as STA by [STAThread] attribute.
        // Do NOT call CoInitializeEx — it would fail with RPC_E_CHANGED_MODE.
        var factory = new WidgetProviderClassFactory();
        Guid clsid = typeof(OmniDownWidgetProvider).GUID;

        int hr = Ole32.CoRegisterClassObject(
            clsid,
            factory,
            Ole32.CLSCTX_LOCAL_SERVER,
            Ole32.REGCLS_MULTIPLEUSE,
            out int cookie);

        if (hr < 0)
        {
            return;
        }

        Ole32.CoResumeClassObjects();

        // ManualResetEvent (the "fat" one) uses CoWaitForMultipleHandles
        // internally on STA threads, which pumps COM messages.
        // ManualResetEventSlim does NOT — COM calls would deadlock.
        using var done = new ManualResetEvent(false);
        done.WaitOne();

        Ole32.CoRevokeClassObject(cookie);
    }
}
