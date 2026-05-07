using System;
using System.Runtime.InteropServices;

namespace OmniDown.Services.Shell;

internal sealed class TaskbarProgressService
{
    private readonly nint _windowHandle;
    private readonly ITaskbarList3? _taskbarList;

    public TaskbarProgressService(nint windowHandle)
    {
        _windowHandle = windowHandle;

        if (_windowHandle == 0)
        {
            return;
        }

        try
        {
            Type? taskbarListType = Type.GetTypeFromCLSID(TaskbarListClassId);
            _taskbarList = taskbarListType is null
                ? null
                : Activator.CreateInstance(taskbarListType) as ITaskbarList3;
            _taskbarList?.HrInit();
        }
        catch
        {
            _taskbarList = null;
        }
    }

    public void SetProgress(double progress)
    {
        if (_taskbarList is null || _windowHandle == 0)
        {
            return;
        }

        ulong completed = (ulong)Math.Clamp(Math.Round(progress * 1000), 0, 1000);
        try
        {
            _taskbarList.SetProgressState(_windowHandle, TaskbarProgressState.Normal);
            _taskbarList.SetProgressValue(_windowHandle, completed, 1000);
        }
        catch
        {
        }
    }

    public void Clear()
    {
        if (_taskbarList is null || _windowHandle == 0)
        {
            return;
        }

        try
        {
            _taskbarList.SetProgressState(_windowHandle, TaskbarProgressState.NoProgress);
        }
        catch
        {
        }
    }

    private static readonly Guid TaskbarListClassId = new("56FDF344-FD6D-11d0-958A-006097C9A090");

    [ComImport]
    [Guid("EA1AFB91-9E28-4B86-90E9-9E9F8A5EEFAF")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ITaskbarList3
    {
        void HrInit();
        void AddTab(nint hwnd);
        void DeleteTab(nint hwnd);
        void ActivateTab(nint hwnd);
        void SetActiveAlt(nint hwnd);
        void MarkFullscreenWindow(nint hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);
        void SetProgressValue(nint hwnd, ulong completed, ulong total);
        void SetProgressState(nint hwnd, TaskbarProgressState state);
    }

    private enum TaskbarProgressState
    {
        NoProgress = 0,
        Indeterminate = 1,
        Normal = 2,
        Error = 4,
        Paused = 8
    }
}
