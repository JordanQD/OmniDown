using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
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
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> options = new()
        {
            ["dir"] = directory
        };

        if (!string.IsNullOrWhiteSpace(outputFileName))
        {
            options["out"] = outputFileName.Trim();
        }

        return await SendAsync<string>("aria2.addUri", [new[] { uri }, options], cancellationToken);
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

        var request = new Aria2RpcRequest(
            JsonRpc: "2.0",
            Id: Interlocked.Increment(ref _nextRequestId).ToString(),
            Method: method,
            Params: parameters);

        string payload = JsonSerializer.Serialize(request, _jsonOptions);
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
            Aria2RpcResponse<T>? rpcResponse = await JsonSerializer.DeserializeAsync<Aria2RpcResponse<T>>(
                responseStream,
                _jsonOptions,
                cancellationToken);

            if (rpcResponse is null)
            {
                throw new InvalidOperationException("aria2 returned an empty RPC response.");
            }

            if (rpcResponse.Error is not null)
            {
                throw new InvalidOperationException($"aria2 RPC error {rpcResponse.Error.Code}: {rpcResponse.Error.Message}");
            }

            return rpcResponse.Result ?? throw new InvalidOperationException("aria2 RPC response did not include a result.");
        }
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
        "dir",
        "files"
    ];

    private sealed record Aria2RpcRequest(
        [property: JsonPropertyName("jsonrpc")] string JsonRpc,
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("params")] IReadOnlyList<object> Params);

    private sealed record Aria2RpcResponse<T>
    {
        [JsonPropertyName("result")]
        public T? Result { get; init; }

        [JsonPropertyName("error")]
        public Aria2RpcError? Error { get; init; }
    }

    private sealed record Aria2RpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; init; }

        [JsonPropertyName("message")]
        public string Message { get; init; } = string.Empty;
    }
}
