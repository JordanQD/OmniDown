using OmniDown.Services.Rpc;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmniDown.Services.Localization;

public enum UserErrorContext
{
    General,
    AddTask,
    TaskOperation,
    RpcRefresh,
    SpeedLimit,
    TaskSpeedLimit,
    DownloadSettingsSync,
    EngineImport,
    SessionClear,
    TrackerSync,
    BrowserExtensionApi,
    BrowserExtensionAdd,
    AutoShutdown,
    RestoreTasks,
    EngineUpdateCheck,
    Ed2kSearch,
    Ed2kSearchDownload
}

public sealed record UserErrorPresentation(string Message, string TechnicalDetails);

public static class UserErrorMessages
{
    public static UserErrorPresentation Create(UserErrorContext context, Exception exception)
    {
        string action = Strings.Get(GetActionResourceKey(context));
        string detail = Strings.Get(GetDetailResourceKey(exception));
        return new UserErrorPresentation(
            Strings.Format("UserErrorCombinedMessage", action, detail),
            exception.ToString());
    }

    public static UserErrorPresentation Create(UserErrorContext context, string? technicalDetails)
    {
        string action = Strings.Get(GetActionResourceKey(context));
        string detail = Strings.Get("UserErrorDetailUnknown");
        return new UserErrorPresentation(
            Strings.Format("UserErrorCombinedMessage", action, detail),
            technicalDetails ?? string.Empty);
    }

    private static string GetActionResourceKey(UserErrorContext context) => context switch
    {
        UserErrorContext.AddTask => "UserErrorActionAddTask",
        UserErrorContext.TaskOperation => "UserErrorActionTaskOperation",
        UserErrorContext.RpcRefresh => "UserErrorActionRpcRefresh",
        UserErrorContext.SpeedLimit => "UserErrorActionSpeedLimit",
        UserErrorContext.TaskSpeedLimit => "UserErrorActionTaskSpeedLimit",
        UserErrorContext.DownloadSettingsSync => "UserErrorActionDownloadSettingsSync",
        UserErrorContext.EngineImport => "UserErrorActionEngineImport",
        UserErrorContext.SessionClear => "UserErrorActionSessionClear",
        UserErrorContext.TrackerSync => "UserErrorActionTrackerSync",
        UserErrorContext.BrowserExtensionApi => "UserErrorActionBrowserExtensionApi",
        UserErrorContext.BrowserExtensionAdd => "UserErrorActionBrowserExtensionAdd",
        UserErrorContext.AutoShutdown => "UserErrorActionAutoShutdown",
        UserErrorContext.RestoreTasks => "UserErrorActionRestoreTasks",
        UserErrorContext.EngineUpdateCheck => "UserErrorActionEngineUpdateCheck",
        UserErrorContext.Ed2kSearch => "UserErrorActionEd2kSearch",
        UserErrorContext.Ed2kSearchDownload => "UserErrorActionEd2kSearchDownload",
        _ => "UserErrorActionGeneral"
    };

    private static string GetDetailResourceKey(Exception exception)
    {
        foreach (Exception current in EnumerateExceptionChain(exception))
        {
            if (current is Aria2RpcException rpcException)
            {
                return rpcException.FailureKind switch
                {
                    Aria2RpcFailureKind.Timeout => "UserErrorDetailRpcTimeout",
                    Aria2RpcFailureKind.Unavailable => "UserErrorDetailRpcUnavailable",
                    Aria2RpcFailureKind.HttpError => "UserErrorDetailRpcHttpError",
                    Aria2RpcFailureKind.RpcRejected => "UserErrorDetailRpcRejected",
                    Aria2RpcFailureKind.InvalidResponse => "UserErrorDetailRpcInvalidResponse",
                    _ => "UserErrorDetailUnknown"
                };
            }

            if (current is TimeoutException or TaskCanceledException)
            {
                return "UserErrorDetailTimeout";
            }

            if (current is UnauthorizedAccessException)
            {
                return "UserErrorDetailPermissionDenied";
            }

            if (current is FileNotFoundException or DirectoryNotFoundException)
            {
                return "UserErrorDetailPathUnavailable";
            }

            if (current is IOException ioException)
            {
                int nativeError = ioException.HResult & 0xFFFF;
                return nativeError is 0x27 or 0x70
                    ? "UserErrorDetailDiskFull"
                    : "UserErrorDetailFileIo";
            }

            if (current is HttpRequestException or SocketException)
            {
                return "UserErrorDetailNetworkUnavailable";
            }

            if (current is JsonException or InvalidDataException or FormatException)
            {
                return "UserErrorDetailInvalidData";
            }

            if (current is NotSupportedException)
            {
                return "UserErrorDetailUnsupportedFeature";
            }
        }

        return "UserErrorDetailUnknown";
    }

    private static System.Collections.Generic.IEnumerable<Exception> EnumerateExceptionChain(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            yield return current;
        }
    }
}
