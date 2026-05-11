namespace OmniDown.Services.Shell;

using System;
using System.Runtime.InteropServices;

internal static class SystemSleepOverrideService
{
    public static void KeepSystemAwake()
    {
        SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired);
    }

    public static void Release()
    {
        SetThreadExecutionState(ExecutionState.Continuous);
    }

    [DllImport("kernel32.dll")]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    [Flags]
    private enum ExecutionState : uint
    {
        SystemRequired = 0x00000001,
        Continuous = 0x80000000
    }
}
