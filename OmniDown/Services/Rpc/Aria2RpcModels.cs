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

    [JsonPropertyName("bittorrent")]
    public JsonElement? BitTorrent { get; init; }

    [JsonPropertyName("dir")]
    public string Directory { get; init; } = string.Empty;

    [JsonPropertyName("files")]
    public IReadOnlyList<Aria2FileStatus> Files { get; init; } = [];
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
}
