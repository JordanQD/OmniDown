using Microsoft.Windows.Widgets;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace OmniDown.Services.Widgets;

public sealed class WidgetCardBuilder
{
    private static readonly JsonSerializerOptions CardOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string BuildCard(WidgetSnapshot? snapshot, WidgetSize size)
    {
        if (snapshot is null || !snapshot.EngineRunning)
        {
            return BuildNotRunningCard();
        }

        return size switch
        {
            WidgetSize.Small => BuildSmallCard(snapshot),
            WidgetSize.Medium => BuildMediumCard(snapshot),
            WidgetSize.Large => BuildLargeCard(snapshot),
            _ => BuildMediumCard(snapshot)
        };
    }

    private static string BuildNotRunningCard()
    {
        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = new object[]
            {
                new { type = "TextBlock", text = "OmniDown", weight = "bolder", size = "medium" },
                new { type = "TextBlock", text = "Engine not running", isSubtle = true, wrap = true }
            },
            actions = new object[]
            {
                new { type = "Action.OpenUrl", title = "Open OmniDown", url = "omnidown://open" }
            }
        };

        return JsonSerializer.Serialize(card, CardOptions);
    }

    private static string BuildSmallCard(WidgetSnapshot snapshot)
    {
        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = new object[]
            {
                new { type = "TextBlock", text = "OmniDown", weight = "bolder", size = "small" },
                new { type = "TextBlock", text = FormatSpeed(snapshot.DownloadSpeed), size = "large", weight = "bolder", spacing = "small" },
                new { type = "TextBlock", text = $"{snapshot.ActiveCount} active", isSubtle = true, spacing = "small", wrap = true }
            },
            actions = new object[]
            {
                new { type = "Action.OpenUrl", title = "Open OmniDown", url = "omnidown://open" }
            }
        };

        return JsonSerializer.Serialize(card, CardOptions);
    }

    private static string BuildMediumCard(WidgetSnapshot snapshot)
    {
        var body = new List<object>
        {
            new { type = "TextBlock", text = "OmniDown", weight = "bolder", size = "small" },
            new
            {
                type = "ColumnSet",
                columns = new object[]
                {
                    BuildStatColumn("Download", FormatSpeed(snapshot.DownloadSpeed)),
                    BuildStatColumn("Active", snapshot.ActiveCount.ToString()),
                    BuildStatColumn("Done", snapshot.CompletedCount.ToString())
                }
            }
        };

        for (int i = 0; i < snapshot.Tasks.Count && i < 2; i++)
        {
            body.Add(BuildTaskRow(snapshot.Tasks[i]));
        }

        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = body.ToArray(),
            actions = new object[]
            {
                new { type = "Action.OpenUrl", title = "Open OmniDown", url = "omnidown://open" }
            }
        };

        return JsonSerializer.Serialize(card, CardOptions);
    }

    private static string BuildLargeCard(WidgetSnapshot snapshot)
    {
        var body = new List<object>
        {
            new { type = "TextBlock", text = "OmniDown", weight = "bolder", size = "medium" },
            new
            {
                type = "ColumnSet",
                columns = new object[]
                {
                    BuildStatColumn("Download", FormatSpeed(snapshot.DownloadSpeed)),
                    BuildStatColumn("Upload", FormatSpeed(snapshot.UploadSpeed)),
                    BuildStatColumn("Active", snapshot.ActiveCount.ToString()),
                    BuildStatColumn("Done", snapshot.CompletedCount.ToString())
                }
            }
        };

        if (snapshot.PausedCount > 0 || snapshot.ErrorCount > 0)
        {
            body.Add(new
            {
                type = "ColumnSet",
                columns = new object[]
                {
                    BuildStatColumn("Paused", snapshot.PausedCount.ToString()),
                    BuildStatColumn("Errors", snapshot.ErrorCount.ToString())
                }
            });
        }

        for (int i = 0; i < snapshot.Tasks.Count; i++)
        {
            body.Add(BuildTaskRow(snapshot.Tasks[i]));
        }

        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = body.ToArray(),
            actions = new object[]
            {
                new { type = "Action.OpenUrl", title = "Open OmniDown", url = "omnidown://open" }
            }
        };

        return JsonSerializer.Serialize(card, CardOptions);
    }

    private static object BuildStatColumn(string label, string value)
    {
        return new
        {
            type = "Column",
            width = "auto",
            items = new object[]
            {
                new { type = "TextBlock", text = value, weight = "bolder", size = "medium", horizontalAlignment = "center" },
                new { type = "TextBlock", text = label, isSubtle = true, size = "small", horizontalAlignment = "center", spacing = "none" }
            }
        };
    }

    private static object BuildTaskRow(WidgetTaskSummary task)
    {
        return new
        {
            type = "ColumnSet",
            spacing = "small",
            columns = new object[]
            {
                new
                {
                    type = "Column",
                    width = "stretch",
                    items = new object[]
                    {
                        new { type = "TextBlock", text = TruncateText(task.Name, 40), wrap = false, size = "small" },
                        new { type = "TextBlock", text = $"{task.Progress:0}% - {FormatBytes(task.CompletedLength)}", isSubtle = true, size = "small", spacing = "none" }
                    }
                }
            }
        };
    }

    private static string FormatSpeed(long bytesPerSecond)
    {
        string[] units = { "B/s", "KB/s", "MB/s", "GB/s" };
        double speed = bytesPerSecond;
        int unitIndex = 0;
        while (speed >= 1024 && unitIndex < units.Length - 1)
        {
            speed /= 1024;
            unitIndex++;
        }

        return $"{speed:0.#} {units[unitIndex]}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }

    private static string TruncateText(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
