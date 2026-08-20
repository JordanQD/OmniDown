using OmniDown.Services.Localization;
using OmniDown.Services.Rpc;
using System;
using System.Globalization;

namespace OmniDown.Models;

public sealed class Ed2kSearchResultEntry
{
    public Ed2kSearchResultEntry(Aria2Ed2kSearchResult result)
    {
        Ed2kLink = result.Ed2kLink.Trim();
        Hash = result.Hash.Trim();
        Name = string.IsNullOrWhiteSpace(result.Name) ? Hash : result.Name.Trim();
        Length = ParseInt64(result.Length);
        SourceCount = ParseInt32(result.SourceCount);
        CompleteSourceCount = ParseInt32(result.CompleteSourceCount);
        string network = string.IsNullOrWhiteSpace(result.SourceNetwork)
            ? Strings.Get("Ed2kSearchUnknownNetworkText")
            : result.SourceNetwork.Trim();
        Summary = Strings.Format(
            "Ed2kSearchResultSummaryText",
            FormatBytes(Length),
            SourceCount,
            CompleteSourceCount,
            network);
    }

    public string Name { get; }

    public string Hash { get; }

    public string Ed2kLink { get; }

    public long Length { get; }

    public int SourceCount { get; }

    public int CompleteSourceCount { get; }

    public string Summary { get; }

    private static long ParseInt64(string value) =>
        long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result)
            ? Math.Max(result, 0)
            : 0;

    private static int ParseInt32(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result)
            ? Math.Max(result, 0)
            : 0;

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        int unitIndex = 0;
        while (size >= 1024 && unitIndex < units.Length - 1)
        {
            size /= 1024;
            unitIndex++;
        }

        return $"{size:0.#} {units[unitIndex]}";
    }
}
