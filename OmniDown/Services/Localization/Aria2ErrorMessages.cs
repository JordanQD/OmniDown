using System;

namespace OmniDown.Services.Localization;

public static class Aria2ErrorMessages
{
    public static string FormatTaskError(string? errorCode)
    {
        if (int.TryParse(errorCode, out int code) && code is >= 1 and <= 32 && code != 31)
        {
            return Strings.Format("TaskStatusErrorWithCode", code, GetTaskError(errorCode));
        }

        return Strings.Format("TaskStatusErrorWithReason", GetTaskError(errorCode));
    }

    public static string GetTaskError(string? errorCode)
    {
        if (int.TryParse(errorCode, out int code) && code is >= 1 and <= 32 && code != 31)
        {
            return Strings.Get($"Aria2TaskError{code}");
        }

        return Strings.Get("Aria2TaskErrorUnknown");
    }
}
