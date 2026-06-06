using OmniDown.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniDown.Services.Widgets;

public sealed record WidgetTaskSummary(
    string Gid,
    string Name,
    string Status,
    double Progress,
    long DownloadSpeed,
    long CompletedLength,
    long TotalLength);

public sealed record WidgetSnapshot(
    DateTimeOffset UpdatedAt,
    bool EngineRunning,
    long DownloadSpeed,
    long UploadSpeed,
    int ActiveCount,
    int WaitingCount,
    int PausedCount,
    int CompletedCount,
    int ErrorCount,
    IReadOnlyList<WidgetTaskSummary> Tasks)
{
    public static WidgetSnapshot FromTasks(
        IEnumerable<DownloadTask> tasks,
        long downloadSpeed,
        long uploadSpeed,
        bool engineRunning)
    {
        List<DownloadTask> taskList = tasks.ToList();

        return new WidgetSnapshot(
            UpdatedAt: DateTimeOffset.Now,
            EngineRunning: engineRunning,
            DownloadSpeed: downloadSpeed,
            UploadSpeed: uploadSpeed,
            ActiveCount: taskList.Count(IsActiveTask),
            WaitingCount: taskList.Count(t => t.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase)),
            PausedCount: taskList.Count(t => t.Status.Contains("paused", StringComparison.OrdinalIgnoreCase)),
            CompletedCount: taskList.Count(IsCompletedTask),
            ErrorCount: taskList.Count(IsErrorTask),
            Tasks: taskList
                .Where(t => IsActiveTask(t) || t.Status.Contains("waiting", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(t => t.DownloadSpeed)
                .ThenByDescending(t => t.Progress)
                .Take(5)
                .Select(t => new WidgetTaskSummary(
                    t.Gid,
                    t.Name,
                    t.Status,
                    t.Progress,
                    t.DownloadSpeed,
                    t.CompletedLength,
                    t.TotalLength))
                .ToArray());
    }

    private static bool IsActiveTask(DownloadTask t) =>
        t.Status.Contains("download", StringComparison.OrdinalIgnoreCase);

    private static bool IsCompletedTask(DownloadTask t) =>
        t.Status.Contains("complete", StringComparison.OrdinalIgnoreCase);

    private static bool IsErrorTask(DownloadTask t) =>
        t.Status.Contains("error", StringComparison.OrdinalIgnoreCase);
}
