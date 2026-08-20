using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniDown.Services.Rpc;

public sealed record Aria2TaskStatus
{
    [JsonPropertyName("gid")]
    public string Gid { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("totalLength")]
    public string TotalLength { get; init; } = "0";

    [JsonPropertyName("completedLength")]
    public string CompletedLength { get; init; } = "0";

    [JsonPropertyName("downloadSpeed")]
    public string DownloadSpeed { get; init; } = "0";

    [JsonPropertyName("uploadSpeed")]
    public string UploadSpeed { get; init; } = "0";

    [JsonPropertyName("errorCode")]
    public string ErrorCode { get; init; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; init; } = string.Empty;

    [JsonPropertyName("bittorrent")]
    public JsonElement? BitTorrent { get; init; }

    [JsonPropertyName("ed2k")]
    public Aria2Ed2kInfo? Ed2k { get; init; }

    [JsonPropertyName("dir")]
    public string Directory { get; init; } = string.Empty;

    [JsonPropertyName("files")]
    public IReadOnlyList<Aria2FileStatus> Files { get; init; } = [];
}

public sealed record Aria2Ed2kInfo
{
    [JsonPropertyName("searchActive")]
    public bool SearchActive { get; init; }

    [JsonPropertyName("searchMoreResults")]
    public bool SearchMoreResults { get; init; }

    [JsonPropertyName("searchResultCount")]
    public string SearchResultCount { get; init; } = "0";

    [JsonPropertyName("ed2kLink")]
    public string Ed2kLink { get; init; } = string.Empty;

    [JsonPropertyName("hash")]
    public string Hash { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("length")]
    public string Length { get; init; } = "0";
}

public sealed record Aria2VersionInfo
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("enabledFeatures")]
    public IReadOnlyList<string> EnabledFeatures { get; init; } = [];
}

public sealed record Aria2Ed2kSearchResults
{
    [JsonPropertyName("gid")]
    public string Gid { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("moreResults")]
    public bool MoreResults { get; init; }

    [JsonPropertyName("results")]
    public IReadOnlyList<Aria2Ed2kSearchResult> Results { get; init; } = [];
}

public sealed record Aria2Ed2kSearchResult
{
    [JsonPropertyName("hash")]
    public string Hash { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("length")]
    public string Length { get; init; } = "0";

    [JsonPropertyName("sourceCount")]
    public string SourceCount { get; init; } = "0";

    [JsonPropertyName("completeSourceCount")]
    public string CompleteSourceCount { get; init; } = "0";

    [JsonPropertyName("fileType")]
    public string FileType { get; init; } = string.Empty;

    [JsonPropertyName("extension")]
    public string Extension { get; init; } = string.Empty;

    [JsonPropertyName("sourceNetwork")]
    public string SourceNetwork { get; init; } = string.Empty;

    [JsonPropertyName("ed2kLink")]
    public string Ed2kLink { get; init; } = string.Empty;
}

public sealed record Aria2FileStatus
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("uris")]
    public IReadOnlyList<Aria2UriStatus> Uris { get; init; } = [];
}

public sealed record Aria2UriStatus
{
    [JsonPropertyName("uri")]
    public string Uri { get; init; } = string.Empty;
}

public sealed record Aria2GlobalStat
{
    [JsonPropertyName("downloadSpeed")]
    public string DownloadSpeed { get; init; } = "0";

    [JsonPropertyName("uploadSpeed")]
    public string UploadSpeed { get; init; } = "0";

    [JsonPropertyName("numActive")]
    public string NumActive { get; init; } = "0";

    [JsonPropertyName("numWaiting")]
    public string NumWaiting { get; init; } = "0";

    [JsonPropertyName("numStopped")]
    public string NumStopped { get; init; } = "0";

    [JsonPropertyName("numStoppedTotal")]
    public string NumStoppedTotal { get; init; } = "0";
}
