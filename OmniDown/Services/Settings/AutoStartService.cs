namespace OmniDown.Services.Settings;

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;

internal sealed class AutoStartService
{
    private const string StartupTaskId = "OmniDownStartupTask";
    private const string RegistryValueName = "OmniDown";
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsEnabled()
    {
        StartupTask? startupTask = TryGetStartupTask();
        if (startupTask is not null)
        {
            return startupTask.State is StartupTaskState.Enabled;
        }

        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return runKey?.GetValue(RegistryValueName) is string value &&
                value.Contains(GetExecutablePath(), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public async Task<AutoStartUpdateResult> SetEnabledAsync(bool isEnabled)
    {
        StartupTask? startupTask = TryGetStartupTask();
        if (startupTask is not null)
        {
            if (isEnabled)
            {
                StartupTaskState state = await startupTask.RequestEnableAsync();
                return new AutoStartUpdateResult(
                    IsEnabled: state is StartupTaskState.Enabled,
                    RequiresUserPermission: state is not StartupTaskState.Enabled);
            }

            startupTask.Disable();
            return new AutoStartUpdateResult(IsEnabled: false, RequiresUserPermission: false);
        }

        SetRegistryAutoStartEnabled(isEnabled);
        return new AutoStartUpdateResult(IsEnabled: IsEnabled(), RequiresUserPermission: false);
    }

    private static StartupTask? TryGetStartupTask()
    {
        try
        {
            return StartupTask.GetAsync(StartupTaskId).AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    private static void SetRegistryAutoStartEnabled(bool isEnabled)
    {
        try
        {
            using RegistryKey? runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (runKey is null)
            {
                return;
            }

            if (isEnabled)
            {
                runKey.SetValue(RegistryValueName, $"\"{GetExecutablePath()}\"");
            }
            else
            {
                runKey.DeleteValue(RegistryValueName, throwOnMissingValue: false);
            }
        }
        catch
        {
            // Autostart availability depends on deployment context and registry access.
        }
    }

    private static string GetExecutablePath()
    {
        return Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
    }
}
