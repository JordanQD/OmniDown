using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace OmniDown.Services.Settings;

public static class ProtocolAssociationService
{
    private const string ApplicationName = "OmniDown";

    public static void Synchronize(bool magnetEnabled, bool thunderEnabled, bool omniDownEnabled)
    {
        SynchronizeProtocol("magnet", "Magnet", magnetEnabled);
        SynchronizeProtocol("thunder", "Thunder", thunderEnabled);
        SynchronizeProtocol("omnidown", "OmniDown", omniDownEnabled);
    }

    public static bool IsRegistered(string protocol)
    {
        try
        {
            using RegistryKey? commandKey = Registry.CurrentUser.OpenSubKey(GetCommandKeyPath(protocol));
            string command = commandKey?.GetValue(null)?.ToString() ?? string.Empty;
            return IsOwnCommand(command);
        }
        catch
        {
            return false;
        }
    }

    private static void SynchronizeProtocol(string protocol, string displayName, bool enabled)
    {
        if (enabled)
        {
            RegisterProtocol(protocol, displayName);
            return;
        }

        UnregisterProtocolIfOwned(protocol);
    }

    private static void RegisterProtocol(string protocol, string displayName)
    {
        using RegistryKey protocolKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{protocol}");
        protocolKey.SetValue(null, $"URL:{displayName} Protocol");
        protocolKey.SetValue("URL Protocol", string.Empty);

        using RegistryKey applicationKey = protocolKey.CreateSubKey("Application");
        applicationKey.SetValue("ApplicationName", ApplicationName);

        using RegistryKey defaultIconKey = protocolKey.CreateSubKey("DefaultIcon");
        defaultIconKey.SetValue(null, $"{GetExecutablePath()},0");

        using RegistryKey commandKey = protocolKey.CreateSubKey(@"shell\open\command");
        commandKey.SetValue(null, $"\"{GetExecutablePath()}\" \"%1\"");
    }

    private static void UnregisterProtocolIfOwned(string protocol)
    {
        try
        {
            using RegistryKey? commandKey = Registry.CurrentUser.OpenSubKey(GetCommandKeyPath(protocol));
            string command = commandKey?.GetValue(null)?.ToString() ?? string.Empty;
            if (!IsOwnCommand(command))
            {
                return;
            }

            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{protocol}", throwOnMissingSubKey: false);
        }
        catch
        {
            // Protocol registration is best-effort; Windows default app selection remains user controlled.
        }
    }

    private static string GetCommandKeyPath(string protocol)
    {
        return $@"Software\Classes\{protocol}\shell\open\command";
    }

    private static bool IsOwnCommand(string command)
    {
        return command.Contains(GetExecutablePath(), StringComparison.OrdinalIgnoreCase) ||
            command.Contains("OmniDown.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetExecutablePath()
    {
        return Process.GetCurrentProcess().MainModule?.FileName ??
            Path.Combine(AppContext.BaseDirectory, "OmniDown.exe");
    }
}
