using Microsoft.Windows.ApplicationModel.Resources;

namespace OmniDown.Services.Localization;

public static class Strings
{
    private static readonly ResourceLoader ResourceLoader = new();

    public static string Get(string key)
    {
        string value = ResourceLoader.GetString(key);
        return string.IsNullOrEmpty(value) ? key : value;
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}
