namespace OmniDown.Services.Settings;

using OmniDown.Services.Storage;
using System.IO;
using System.Text.Json;

internal static class AppPreferencesStore
{
    private static readonly string PreferencesPath = Path.Combine(AppPaths.LocalDataDirectory, "app-preferences.json");

    public static bool EngineAutoUpdateEnabled
    {
        get => Read();
        set => Save(value);
    }

    private static bool Read()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                return JsonSerializer.Deserialize<bool>(File.ReadAllText(PreferencesPath));
            }

            object? legacyValue = Windows.Storage.ApplicationData.Current.LocalSettings.Values["EngineAutoUpdateEnabled"];
            if (legacyValue is bool legacyEnabled)
            {
                Save(legacyEnabled);
                return legacyEnabled;
            }
        }
        catch
        {
        }

        return false;
    }

    private static void Save(bool enabled)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
            File.WriteAllText(PreferencesPath, JsonSerializer.Serialize(enabled));
        }
        catch
        {
            // Preferences persistence is best-effort.
        }
    }
}
