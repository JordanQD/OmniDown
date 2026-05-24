using OmniDown.Services.Storage;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace OmniDown.Services.Widgets;

public sealed class WidgetSnapshotStore
{
    private readonly string _snapshotPath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public WidgetSnapshotStore()
    {
        _snapshotPath = Path.Combine(AppPaths.LocalDataDirectory, "widget-snapshot.json");
    }

    public async Task SaveAsync(WidgetSnapshot snapshot)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_snapshotPath)!);
            string json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(_snapshotPath, json);
        }
        catch
        {
            // Best-effort: widget will show stale data if save fails.
        }
    }

    public WidgetSnapshot? Load()
    {
        try
        {
            if (!File.Exists(_snapshotPath))
            {
                return null;
            }

            string json = File.ReadAllText(_snapshotPath);
            return JsonSerializer.Deserialize<WidgetSnapshot>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
