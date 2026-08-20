using OmniDown.Services.Logging;
using OmniDown.Services.Rpc;
using OmniDown.Services.Storage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OmniDown.Services.Ed2k;

public sealed record Ed2kSearchProgress(
    TimeSpan Elapsed,
    TimeSpan Duration,
    IReadOnlyList<Aria2Ed2kSearchResult> Results);

public sealed class Ed2kSearchService(Aria2RpcClient rpcClient)
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    public async Task<IReadOnlyList<Aria2Ed2kSearchResult>> SearchAsync(
        string keyword,
        string fileType,
        int minimumSources,
        TimeSpan duration,
        IProgress<Ed2kSearchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string normalizedKeyword = keyword.Trim();
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            throw new ArgumentException("ED2K search keyword is empty.", nameof(keyword));
        }

        string searchDirectory = Path.Combine(AppPaths.Ed2kSearchDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(searchDirectory);
        string gid = string.Empty;
        IReadOnlyList<Aria2Ed2kSearchResult> latestResults = [];
        try
        {
            Dictionary<string, string> options = new()
            {
                ["dir"] = searchDirectory,
                ["minSourceCount"] = Math.Max(minimumSources, 1).ToString(CultureInfo.InvariantCulture)
            };
            if (!string.IsNullOrWhiteSpace(fileType))
            {
                options["fileType"] = fileType;
            }

            AddBootstrapOption(options, "ed2k-server-list", AppPaths.Ed2kServerMetPath);
            AddBootstrapOption(options, "ed2k-node-list", AppPaths.Ed2kNodesDatPath);

            gid = await rpcClient.StartEd2kSearchAsync(normalizedKeyword, options, cancellationToken);
            DateTimeOffset started = DateTimeOffset.UtcNow;
            while (DateTimeOffset.UtcNow - started < duration)
            {
                await Task.Delay(PollInterval, cancellationToken);
                Aria2Ed2kSearchResults payload = await rpcClient.GetEd2kSearchResultsAsync(gid, cancellationToken);
                latestResults = NormalizeResults(payload.Results);
                progress?.Report(new Ed2kSearchProgress(DateTimeOffset.UtcNow - started, duration, latestResults));
            }

            return latestResults;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(gid))
            {
                await CleanupSearchTaskAsync(gid);
            }

            TryDeleteSearchDirectory(searchDirectory);
        }
    }

    private async Task CleanupSearchTaskAsync(string gid)
    {
        try
        {
            await rpcClient.ForceRemoveAsync(gid, CancellationToken.None);
            return;
        }
        catch (Exception ex)
        {
            AppLogger.Debug("ED2K.SearchCleanup", $"forceRemove skipped gid={gid}: {ex.Message}");
        }

        try
        {
            await rpcClient.RemoveDownloadResultAsync(gid, CancellationToken.None);
        }
        catch (Exception ex)
        {
            AppLogger.Debug("ED2K.SearchCleanup", $"removeDownloadResult skipped gid={gid}: {ex.Message}");
        }
    }

    private static IReadOnlyList<Aria2Ed2kSearchResult> NormalizeResults(
        IReadOnlyList<Aria2Ed2kSearchResult>? results)
    {
        return (results ?? [])
            .Where(result => !string.IsNullOrWhiteSpace(result.Ed2kLink))
            .GroupBy(
                result => string.IsNullOrWhiteSpace(result.Hash) ? result.Ed2kLink : result.Hash,
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(result => ParseCount(result.SourceCount)).First())
            .OrderByDescending(result => ParseCount(result.SourceCount))
            .ThenBy(result => result.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private static int ParseCount(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;

    private static void AddBootstrapOption(Dictionary<string, string> options, string name, string path)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 0)
        {
            options[name] = path;
        }
    }

    private static void TryDeleteSearchDirectory(string path)
    {
        try
        {
            string root = Path.GetFullPath(AppPaths.Ed2kSearchDirectory)
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string candidate = Path.GetFullPath(path);
            if (candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(candidate))
            {
                Directory.Delete(candidate, recursive: true);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Debug("ED2K.SearchCleanup", $"temporary directory cleanup skipped: {ex.Message}");
        }
    }
}
