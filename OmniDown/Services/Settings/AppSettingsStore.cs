namespace OmniDown.Services.Settings;

using OmniDown.Models.Settings;
using OmniDown.Services.Storage;
using System;
using System.IO;
using System.Text.Json;

internal sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _speedLimitSettingsPath = Path.Combine(AppPaths.LocalDataDirectory, "speed-limits.json");
    private readonly string _closeBehaviorSettingsPath = Path.Combine(AppPaths.LocalDataDirectory, "close-behavior.json");
    private readonly string _generalSettingsPath = Path.Combine(AppPaths.LocalDataDirectory, "general-settings.json");

    public GeneralSettings ReadGeneralSettings()
    {
        return Read(_generalSettingsPath, GeneralSettings.Default);
    }

    public void SaveGeneralSettings(GeneralSettings settings)
    {
        Save(_generalSettingsPath, settings);
    }

    public SpeedLimitSettings ReadSpeedLimitSettings()
    {
        return Read(_speedLimitSettingsPath, SpeedLimitSettings.Default);
    }

    public void SaveSpeedLimitSettings(SpeedLimitSettings settings)
    {
        Save(_speedLimitSettingsPath, settings);
    }

    public CloseBehaviorSettings ReadCloseBehaviorSettings()
    {
        return Read(_closeBehaviorSettingsPath, CloseBehaviorSettings.Default);
    }

    public void SaveCloseBehaviorSettings(CloseBehaviorSettings settings)
    {
        Save(_closeBehaviorSettingsPath, settings);
    }

    private static TSettings Read<TSettings>(string path, TSettings fallback)
    {
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TSettings>(json) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static void Save<TSettings>(string path, TSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, SerializerOptions));
        }
        catch
        {
            // Settings persistence is best-effort; defaults remain usable.
        }
    }
}
