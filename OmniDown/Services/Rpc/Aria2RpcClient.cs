using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OmniDown.Services.Rpc;

public sealed class Aria2RpcClient : IDisposable
{
    private readonly HttpClient _httpClient = new(new HttpClientHandler
    {
        UseProxy = false
    })
    {
        Timeout = TimeSpan.FromSeconds(8)
    };
    private Uri _endpoint = new("http://127.0.0.1:6800/jsonrpc");
    private string _secret = string.Empty;
    private int _nextRequestId;

    public string Endpoint => _endpoint.ToString();

    public void Configure(int rpcPort, string rpcSecret)
    {
        _endpoint = new Uri($"http://127.0.0.1:{rpcPort}/jsonrpc");
        _secret = rpcSecret;
    }

    public Task PingAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<object>("aria2.getVersion", [], cancellationToken);
    }

    public async Task<string> AddUriAsync(
        string uri,
        string? outputFileName,
        string directory,
        int splitCount,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["dir"] = directory,
            ["split"] = Math.Clamp(splitCount, 1, 256).ToString(CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(outputFileName))
        {
            options["out"] = outputFileName.Trim();
        }

        return await SendAsync<string>("aria2.addUri", [new[] { uri }, options], cancellationToken);
    }

    public async Task<string> AddTorrentAsync(
        byte[] torrentBytes,
        string directory,
        int splitCount,
        IReadOnlyList<int> selectedFileIndexes,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["dir"] = directory,
            ["split"] = Math.Clamp(splitCount, 1, 256).ToString(CultureInfo.InvariantCulture)
        };

        if (selectedFileIndexes.Count > 0)
        {
            options["select-file"] = string.Join(",", selectedFileIndexes);
        }

        string torrent = Convert.ToBase64String(torrentBytes);
        return await SendAsync<string>("aria2.addTorrent", [torrent, Array.Empty<string>(), options], cancellationToken);
    }

    public Task<IReadOnlyList<Aria2TaskStatus>> TellActiveAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyList<Aria2TaskStatus>>("aria2.tellActive", [TaskKeys], cancellationToken);
    }

    public Task<IReadOnlyList<Aria2TaskStatus>> TellWaitingAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyList<Aria2TaskStatus>>("aria2.tellWaiting", [0, 200, TaskKeys], cancellationToken);
    }

    public Task<IReadOnlyList<Aria2TaskStatus>> TellStoppedAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<IReadOnlyList<Aria2TaskStatus>>("aria2.tellStopped", [0, 200, TaskKeys], cancellationToken);
    }

    public Task<Aria2GlobalStat> GetGlobalStatAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<Aria2GlobalStat>("aria2.getGlobalStat", [], cancellationToken);
    }

    public Task ChangeGlobalOptionAsync(
        Dictionary<string, string> options,
        CancellationToken cancellationToken = default)
    {
        return SendAsync<string>("aria2.changeGlobalOption", [options], cancellationToken);
    }

    public Task PauseAsync(string gid, CancellationToken cancellationToken = default)
    {
        return SendAsync<string>("aria2.pause", [gid], cancellationToken);
    }

    public Task UnpauseAsync(string gid, CancellationToken cancellationToken = default)
    {
        return SendAsync<string>("aria2.unpause", [gid], cancellationToken);
    }

    public Task RemoveAsync(string gid, CancellationToken cancellationToken = default)
    {
        return SendAsync<string>("aria2.remove", [gid], cancellationToken);
    }

    public Task RemoveDownloadResultAsync(string gid, CancellationToken cancellationToken = default)
    {
        return SendAsync<string>("aria2.removeDownloadResult", [gid], cancellationToken);
    }

    public Task SaveSessionAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync<string>("aria2.saveSession", [], cancellationToken);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<T> SendAsync<T>(
        string method,
        IReadOnlyList<object> methodParameters,
        CancellationToken cancellationToken)
    {
        List<object> parameters = [];
        if (!string.IsNullOrWhiteSpace(_secret))
        {
            parameters.Add($"token:{_secret}");
        }

        parameters.AddRange(methodParameters);

        string payload = BuildPayload(
            Interlocked.Increment(ref _nextRequestId).ToString(),
            method,
            parameters);
        using StringContent content = new(payload, Encoding.UTF8, "application/json");
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(_endpoint, content, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"aria2 RPC method {method} timed out after {_httpClient.Timeout.TotalSeconds:0} seconds.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"aria2 RPC method {method} could not reach {_endpoint}: {FormatRequestError(ex)}", ex);
        }

        using (response)
        {
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument rpcResponse = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
            JsonElement root = rpcResponse.RootElement;
            if (root.ValueKind is not JsonValueKind.Object)
            {
                throw new InvalidOperationException("aria2 returned an empty RPC response.");
            }

            if (root.TryGetProperty("error", out JsonElement error) &&
                error.ValueKind is JsonValueKind.Object)
            {
                int code = TryGetInt32(error, "code");
                string message = TryGetString(error, "message");
                throw new InvalidOperationException($"aria2 RPC error {code}: {message}");
            }

            if (!root.TryGetProperty("result", out JsonElement result))
            {
                throw new InvalidOperationException("aria2 RPC response did not include a result.");
            }

            return ReadResult<T>(result);
        }
    }

    private static string BuildPayload(string id, string method, IReadOnlyList<object> parameters)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("jsonrpc", "2.0");
            writer.WriteString("id", id);
            writer.WriteString("method", method);
            writer.WritePropertyName("params");
            writer.WriteStartArray();
            foreach (object parameter in parameters)
            {
                WriteParameter(writer, parameter);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteParameter(Utf8JsonWriter writer, object parameter)
    {
        switch (parameter)
        {
            case string value:
                writer.WriteStringValue(value);
                break;
            case int value:
                writer.WriteNumberValue(value);
                break;
            case string[] values:
                writer.WriteStartArray();
                foreach (string value in values)
                {
                    writer.WriteStringValue(value);
                }

                writer.WriteEndArray();
                break;
            case Dictionary<string, string> options:
                writer.WriteStartObject();
                foreach (KeyValuePair<string, string> option in options)
                {
                    writer.WriteString(option.Key, option.Value);
                }

                writer.WriteEndObject();
                break;
            default:
                throw new NotSupportedException($"aria2 RPC parameter type {parameter.GetType().Name} is not supported.");
        }
    }

    private static T ReadResult<T>(JsonElement result)
    {
        Type resultType = typeof(T);
        if (resultType == typeof(object))
        {
            return (T)(object)new object();
        }

        if (resultType == typeof(string))
        {
            return (T)(object)(result.GetString() ?? string.Empty);
        }

        if (resultType == typeof(Aria2GlobalStat))
        {
            return (T)(object)new Aria2GlobalStat
            {
                DownloadSpeed = TryGetString(result, "downloadSpeed"),
                UploadSpeed = TryGetString(result, "uploadSpeed"),
                NumActive = TryGetString(result, "numActive")
            };
        }

        if (resultType == typeof(IReadOnlyList<Aria2TaskStatus>))
        {
            List<Aria2TaskStatus> tasks = [];
            foreach (JsonElement item in result.EnumerateArray())
            {
                tasks.Add(ReadTaskStatus(item));
            }

            return (T)(object)tasks;
        }

        throw new NotSupportedException($"aria2 RPC result type {resultType.Name} is not supported.");
    }

    private static Aria2TaskStatus ReadTaskStatus(JsonElement item)
    {
        return new Aria2TaskStatus
        {
            Gid = TryGetString(item, "gid"),
            Status = TryGetString(item, "status"),
            TotalLength = TryGetString(item, "totalLength"),
            CompletedLength = TryGetString(item, "completedLength"),
            DownloadSpeed = TryGetString(item, "downloadSpeed"),
            UploadSpeed = TryGetString(item, "uploadSpeed"),
            BitTorrent = item.TryGetProperty("bittorrent", out JsonElement bitTorrent) ? bitTorrent.Clone() : null,
            Directory = TryGetString(item, "dir"),
            Files = ReadFiles(item)
        };
    }

    private static IReadOnlyList<Aria2FileStatus> ReadFiles(JsonElement item)
    {
        if (!item.TryGetProperty("files", out JsonElement files) ||
            files.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        List<Aria2FileStatus> result = [];
        foreach (JsonElement file in files.EnumerateArray())
        {
            result.Add(new Aria2FileStatus
            {
                Path = TryGetString(file, "path"),
                Uris = ReadUris(file)
            });
        }

        return result;
    }

    private static IReadOnlyList<Aria2UriStatus> ReadUris(JsonElement file)
    {
        if (!file.TryGetProperty("uris", out JsonElement uris) ||
            uris.ValueKind is not JsonValueKind.Array)
        {
            return [];
        }

        List<Aria2UriStatus> result = [];
        foreach (JsonElement uri in uris.EnumerateArray())
        {
            result.Add(new Aria2UriStatus
            {
                Uri = TryGetString(uri, "uri")
            });
        }

        return result;
    }

    private static string TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind is JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int TryGetInt32(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) && property.TryGetInt32(out int value)
            ? value
            : 0;
    }

    private static string FormatRequestError(HttpRequestException exception)
    {
        if (exception.InnerException is SocketException socketException)
        {
            return $"{socketException.SocketErrorCode} ({socketException.Message})";
        }

        return exception.InnerException?.Message ?? exception.Message;
    }

    private static readonly string[] TaskKeys =
    [
        "gid",
        "status",
        "totalLength",
        "completedLength",
        "downloadSpeed",
        "uploadSpeed",
        "bittorrent",
        "dir",
        "files"
    ];

}
