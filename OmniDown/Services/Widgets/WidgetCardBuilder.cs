using Microsoft.Windows.Widgets;
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
                new { type = "TextBlock", text = "Engine not running", isSubtle = true, wrap = true },
                BuildStatusRow(
                    BuildStatItem("↑", "0 B/s"),
                    BuildStatItem("↓", "0 B/s")),
                BuildStatusRow(
                    BuildStatItem("▶", "0"),
                    BuildStatItem("✓", "0")),
                BuildOpenActionContainer()
            },
            actions = new object[]
            {
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
                BuildStatusRow(
                    BuildStatItem("↑", FormatSpeed(snapshot.UploadSpeed)),
                    BuildStatItem("↓", FormatSpeed(snapshot.DownloadSpeed))),
                BuildStatusRow(
                    BuildStatItem("▶", snapshot.ActiveCount.ToString()),
                    BuildStatItem("✓", snapshot.CompletedCount.ToString())),
                BuildOpenActionContainer()
            },
            actions = new object[]
            {
            }
        };

        return JsonSerializer.Serialize(card, CardOptions);
    }

    private static string BuildMediumCard(WidgetSnapshot snapshot)
    {
        var body = new List<object>
        {
            BuildStatusRow(
                BuildStatItem("↑", FormatSpeed(snapshot.UploadSpeed)),
                BuildStatItem("↓", FormatSpeed(snapshot.DownloadSpeed))),
            BuildStatusRow(
                BuildStatItem("▶", snapshot.ActiveCount.ToString()),
                BuildStatItem("✓", snapshot.CompletedCount.ToString()))
        };

        for (int i = 0; i < snapshot.Tasks.Count && i < 2; i++)
        {
            body.Add(BuildTaskRow(snapshot.Tasks[i]));
        }

        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = AppendOpenAction(body).ToArray(),
            actions = new object[]
            {
            }
        };

        return JsonSerializer.Serialize(card, CardOptions);
    }

    private static string BuildLargeCard(WidgetSnapshot snapshot)
    {
        var body = new List<object>
        {
            BuildStatusRow(
                BuildStatItem("↑", FormatSpeed(snapshot.UploadSpeed)),
                BuildStatItem("↓", FormatSpeed(snapshot.DownloadSpeed))),
            BuildStatusRow(
                BuildStatItem("▶", snapshot.ActiveCount.ToString()),
                BuildStatItem("✓", snapshot.CompletedCount.ToString()))
        };

        if (snapshot.PausedCount > 0 || snapshot.ErrorCount > 0)
        {
            body.Add(BuildStatusRow(
                BuildStatItem("⏸", snapshot.PausedCount.ToString()),
                BuildStatItem("!", snapshot.ErrorCount.ToString())));
        }

        for (int i = 0; i < snapshot.Tasks.Count; i++)
        {
            body.Add(BuildTaskRow(snapshot.Tasks[i]));
        }

        var card = new
        {
            type = "AdaptiveCard",
            version = "1.5",
            body = AppendOpenAction(body).ToArray(),
            actions = new object[]
            {
            }
        };

        return JsonSerializer.Serialize(card, CardOptions);
    }

    private static object BuildStatusRow(params object[] stats)
    {
        return new
        {
            type = "ColumnSet",
            spacing = "medium",
            columns = stats
        };
    }

    private static object BuildStatItem(string icon, string value)
    {
        return new
        {
            type = "Column",
            width = "stretch",
            items = new object[]
            {
                new
                {
                    type = "ColumnSet",
                    spacing = "none",
                    columns = new object[]
                    {
                        new
                        {
                            type = "Column",
                            width = "auto",
                            items = new object[]
                            {
                                new { type = "TextBlock", text = icon, size = "small", weight = "bolder" }
                            }
                        },
                        new
                        {
                            type = "Column",
                            width = "stretch",
                            items = new object[]
                            {
                                new { type = "TextBlock", text = value, weight = "bolder", size = "medium", wrap = false }
                            }
                        }
                    }
                }
            }
        };
    }

    private static List<object> AppendOpenAction(List<object> body)
    {
        body.Add(BuildOpenActionContainer());
        return body;
    }

    private static object BuildOpenActionContainer()
    {
        return new
        {
            type = "Container",
            spacing = "large",
            selectAction = new { type = "Action.OpenUrl", url = "omnidown://open" },
            items = new object[]
            {
                new
                {
                    type = "TextBlock",
                    text = "Open OmniDown",
                    horizontalAlignment = "center",
                    color = "accent",
                    wrap = false
                }
            }
        };
    }

    private static object BuildTaskRow(WidgetTaskSummary task)
    {
        return new
        {
            type = "Container",
            spacing = "medium",
            separator = true,
            minHeight = "64px",
            items = new object[]
            {
                new { type = "TextBlock", text = TruncateText(task.Name, 46), wrap = false, size = "small", weight = "bolder" },
                new
                {
                    type = "ColumnSet",
                    spacing = "small",
                    columns = new object[]
                    {
                        new
                        {
                            type = "Column",
                            width = "auto",
                            items = new object[]
                            {
                                new { type = "TextBlock", text = $"{task.Progress:0}%", weight = "bolder", size = "small", wrap = false }
                            }
                        },
                        new
                        {
                            type = "Column",
                            width = "stretch",
                            items = new object[]
                            {
                                new
                                {
                                    type = "TextBlock",
                                    text = FormatTaskSize(task),
                                    isSubtle = true,
                                    size = "small",
                                    horizontalAlignment = "right",
                                    wrap = false
                                }
                            }
                        }
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

    private static string FormatTaskSize(WidgetTaskSummary task)
    {
        if (task.TotalLength > 0)
        {
            return $"{FormatBytes(task.CompletedLength)} / {FormatBytes(task.TotalLength)}";
        }

        return FormatBytes(task.CompletedLength);
    }

    private static string TruncateText(string text, int maxLength)
    {
        return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
    }
}
