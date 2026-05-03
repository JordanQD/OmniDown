using Microsoft.Windows.ApplicationModel.Resources;
using System.Runtime.InteropServices;

namespace OmniDown.Services.Localization;

public static class Strings
{
    private static readonly ResourceLoader ResourceLoader = new();

    public static string Get(string key)
    {
        string value = GetStringOrEmpty(key);
        if (string.IsNullOrEmpty(value) && key.Contains('.'))
        {
            value = GetStringOrEmpty(key.Replace('.', '/'));
        }

        return string.IsNullOrEmpty(value) ? key : value;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }

    private static string GetStringOrEmpty(string key)
    {
        try
        {
            return ResourceLoader.GetString(key);
        }
        catch (COMException)
        {
            return string.Empty;
        }
    }
}
