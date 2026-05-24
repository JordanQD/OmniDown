using System;
using System.Runtime.InteropServices;
using Microsoft.Windows.Widgets.Providers;
using OmniDown.Services.Widgets;
using WinRT;

namespace OmniDown;

internal static class Ole32
{
    public const uint CLSCTX_LOCAL_SERVER = 4;
    public const uint REGCLS_MULTIPLEUSE = 1;
    public const uint COINIT_MULTITHREADED = 0;

    [DllImport("ole32.dll")]
    public static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    public static extern void CoUninitialize();

    [DllImport("ole32.dll")]
    public static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
        uint dwClsContext,
        uint flags,
        out int lpdwRegister);

    [DllImport("ole32.dll")]
    public static extern int CoResumeClassObjects();

    [DllImport("ole32.dll")]
    public static extern int CoRevokeClassObject(int dwRegister);
}

[ComImport]
[ComVisible(true)]
[Guid("00000001-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IClassFactory
{
    [PreserveSig]
    int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}

[ComVisible(true)]
internal sealed class WidgetProviderClassFactory : IClassFactory
{
    public int CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
    {
        ppvObject = IntPtr.Zero;
        if (pUnkOuter != IntPtr.Zero)
        {
            return CLASS_E_NOAGGREGATION;
        }

        try
        {
            if (riid == typeof(IWidgetProvider).GUID || riid == IUnknownGuid)
            {
                ppvObject = MarshalInspectable<IWidgetProvider>.FromManaged(new OmniDownWidgetProvider());
                return 0;
            }

            return E_NOINTERFACE;
        }
        catch
        {
            return E_FAIL;
        }
    }

    public int LockServer(bool fLock)
    {
        return 0;
    }

    private const int CLASS_E_NOAGGREGATION = unchecked((int)0x80040110);
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int E_FAIL = unchecked((int)0x80004005);
    private static readonly Guid IUnknownGuid = new("00000000-0000-0000-C000-000000000046");
}
