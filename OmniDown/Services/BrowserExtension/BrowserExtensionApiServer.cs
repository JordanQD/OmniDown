namespace OmniDown.Services.BrowserExtension;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

internal sealed class BrowserExtensionApiServer : IDisposable
{
    private const int MaxRequestBodyChars = 1024 * 1024;
    private readonly Func<BrowserExtensionAddRequest, Task<BrowserExtensionAddResponse>> _addHandler;
    private readonly Func<Task<BrowserExtensionStatResponse>> _statHandler;
    private readonly Func<Task<BrowserExtensionActionResponse>> _pauseAllHandler;
    private readonly Func<Task<BrowserExtensionActionResponse>> _resumeAllHandler;
    private readonly Func<BrowserExtensionVersionResponse> _versionProvider;
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellation;
    private Task? _acceptTask;
    private int _port;
    private string _secret = string.Empty;

    public BrowserExtensionApiServer(
        Func<BrowserExtensionAddRequest, Task<BrowserExtensionAddResponse>> addHandler,
        Func<Task<BrowserExtensionStatResponse>> statHandler,
        Func<Task<BrowserExtensionActionResponse>> pauseAllHandler,
        Func<Task<BrowserExtensionActionResponse>> resumeAllHandler,
        Func<BrowserExtensionVersionResponse> versionProvider)
    {
        _addHandler = addHandler;
        _statHandler = statHandler;
        _pauseAllHandler = pauseAllHandler;
        _resumeAllHandler = resumeAllHandler;
        _versionProvider = versionProvider;
    }

    public bool IsRunning => _listener is not null;

    public int Port => _port;

    public void Start(int port, string secret)
    {
        Stop();

        _port = Math.Clamp(port, 1024, 65535);
        _secret = secret.Trim();
        _cancellation = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Loopback, _port);
        _listener.Start();
        _acceptTask = AcceptLoopAsync(_cancellation.Token);
    }

    public void Stop()
    {
        _cancellation?.Cancel();
        _listener?.Stop();
        _listener = null;
        _acceptTask = null;
        _cancellation?.Dispose();
        _cancellation = null;
    }

    public void Dispose()
    {
        Stop();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _listener is not null)
        {
            TcpClient? client = null;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _ = Task.Run(() => HandleClientAsync(client, cancellationToken), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                client?.Dispose();
                break;
            }
            catch (ObjectDisposedException)
            {
                client?.Dispose();
                break;
            }
            catch
            {
                client?.Dispose();
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        await using NetworkStream stream = client.GetStream();
        using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;

        BrowserExtensionHttpRequest? request = null;
        try
        {
            request = await ReadRequestAsync(reader, cancellationToken);
            if (request is null)
            {
                await WriteJsonAsync(stream, 400, null, new { error = "invalid_request" }, cancellationToken);
                return;
            }

            if (request.Method == "OPTIONS")
            {
                await WriteJsonAsync(stream, 204, request.Origin, null, cancellationToken);
                return;
            }

            switch ((request.Method, request.Path))
            {
                case ("GET", "/ping"):
                    await WriteJsonAsync(stream, 200, request.Origin, new BrowserExtensionPingResponse("ok", _versionProvider().App), cancellationToken);
                    return;
                case ("GET", "/version"):
                    await WriteJsonAsync(stream, 200, request.Origin, _versionProvider(), cancellationToken);
                    return;
                case ("POST", "/add"):
                    await HandleAuthorizedAsync(stream, request, () => HandleAddAsync(request.Body, cancellationToken), cancellationToken);
                    return;
                case ("GET", "/stat"):
                    await HandleAuthorizedAsync(stream, request, _statHandler, cancellationToken);
                    return;
                case ("POST", "/pause-all"):
                    await HandleAuthorizedAsync(stream, request, _pauseAllHandler, cancellationToken);
                    return;
                case ("POST", "/resume-all"):
                    await HandleAuthorizedAsync(stream, request, _resumeAllHandler, cancellationToken);
                    return;
                default:
                    await WriteJsonAsync(stream, 404, request.Origin, new { error = "not_found" }, cancellationToken);
                    return;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await WriteJsonAsync(stream, 500, request?.Origin, new { error = ex.Message }, CancellationToken.None);
        }
        finally
        {
            client.Dispose();
        }
    }

    private async Task HandleAuthorizedAsync<T>(
        NetworkStream stream,
        BrowserExtensionHttpRequest request,
        Func<Task<T>> handler,
        CancellationToken cancellationToken)
    {
        if (!ValidateBearerToken(request.Authorization))
        {
            await WriteJsonAsync(stream, 401, request.Origin, new { error = "unauthorized" }, cancellationToken);
            return;
        }

        T response = await handler();
        await WriteJsonAsync(stream, 200, request.Origin, response, cancellationToken);
    }

    private async Task<BrowserExtensionAddResponse> HandleAddAsync(string body, CancellationToken cancellationToken)
    {
        BrowserExtensionAddRequest? request = JsonSerializer.Deserialize<BrowserExtensionAddRequest>(
            body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (request is null || string.IsNullOrWhiteSpace(request.Url))
        {
            return new BrowserExtensionAddResponse("error", null, "Missing url.");
        }

        return await _addHandler(request with
        {
            Url = request.Url.Trim(),
            Referer = request.Referer?.Trim(),
            Cookie = request.Cookie?.Trim(),
            Filename = request.Filename?.Trim()
        });
    }

    private bool ValidateBearerToken(string authorization)
    {
        if (string.IsNullOrEmpty(_secret))
        {
            return true;
        }

        return string.Equals(authorization, $"Bearer {_secret}", StringComparison.Ordinal);
    }

    private static async Task<BrowserExtensionHttpRequest?> ReadRequestAsync(
        StreamReader reader,
        CancellationToken cancellationToken)
    {
        string? requestLine = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(requestLine))
        {
            return null;
        }

        string[] requestParts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
        if (requestParts.Length < 2)
        {
            return null;
        }

        string method = requestParts[0].Trim().ToUpperInvariant();
        string path = requestParts[1].Split('?', 2)[0].Trim();
        Dictionary<string, string> headers = await ReadHeadersAsync(reader, cancellationToken);
        string body = await ReadBodyAsync(reader, headers, cancellationToken);
        string authorization = headers.TryGetValue("Authorization", out string? value) ? value : string.Empty;
        string origin = headers.TryGetValue("Origin", out string? originValue) ? originValue : string.Empty;
        return new BrowserExtensionHttpRequest(method, path, authorization, origin, body);
    }

    private static async Task<Dictionary<string, string>> ReadHeadersAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            string? line = await reader.ReadLineAsync(cancellationToken);
            if (line is null || line.Length == 0)
            {
                return headers;
            }

            int separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
            {
                continue;
            }

            headers[line[..separatorIndex].Trim()] = line[(separatorIndex + 1)..].Trim();
        }
    }

    private static async Task<string> ReadBodyAsync(
        StreamReader reader,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        if (!headers.TryGetValue("Content-Length", out string? contentLengthText) ||
            !int.TryParse(contentLengthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int contentLength) ||
            contentLength <= 0)
        {
            return string.Empty;
        }

        int length = Math.Min(contentLength, MaxRequestBodyChars);
        char[] buffer = new char[length];
        int offset = 0;
        while (offset < length)
        {
            int read = await reader.ReadBlockAsync(buffer.AsMemory(offset, length - offset), cancellationToken);
            if (read <= 0)
            {
                break;
            }

            offset += read;
        }

        return new string(buffer, 0, offset);
    }

    private static async Task WriteJsonAsync(
        NetworkStream stream,
        int statusCode,
        string? origin,
        object? payload,
        CancellationToken cancellationToken)
    {
        string reason = statusCode switch
        {
            200 => "OK",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            404 => "Not Found",
            _ => "Internal Server Error"
        };
        string body = statusCode == 204 || payload is null
            ? string.Empty
            : JsonSerializer.Serialize(payload);
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string corsOrigin = !string.IsNullOrWhiteSpace(origin) && IsAllowedExtensionOrigin(origin)
            ? $"Access-Control-Allow-Origin: {origin}\r\n"
            : string.Empty;
        string header =
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            corsOrigin +
            "Vary: Origin\r\n" +
            "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
            "Access-Control-Allow-Headers: Content-Type, Authorization\r\n" +
            "Content-Type: application/json; charset=utf-8\r\n" +
            $"Content-Length: {bodyBytes.Length}\r\n" +
            "Connection: close\r\n\r\n";
        byte[] headerBytes = Encoding.ASCII.GetBytes(header);
        await stream.WriteAsync(headerBytes, cancellationToken);
        if (bodyBytes.Length > 0)
        {
            await stream.WriteAsync(bodyBytes, cancellationToken);
        }
    }

    private static bool IsAllowedExtensionOrigin(string? origin)
    {
        return origin?.StartsWith("chrome-extension://", StringComparison.OrdinalIgnoreCase) == true ||
            origin?.StartsWith("moz-extension://", StringComparison.OrdinalIgnoreCase) == true;
    }

    private sealed record BrowserExtensionHttpRequest(
        string Method,
        string Path,
        string Authorization,
        string Origin,
        string Body);
}

internal sealed record BrowserExtensionAddRequest(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("referer")] string? Referer,
    [property: JsonPropertyName("cookie")] string? Cookie,
    [property: JsonPropertyName("filename")] string? Filename);

internal sealed record BrowserExtensionAddResponse(
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("gid")] string? Gid,
    [property: JsonPropertyName("message")] string? Message);

internal sealed record BrowserExtensionPingResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("version")] string Version);

internal sealed record BrowserExtensionVersionResponse(
    [property: JsonPropertyName("app")] string App,
    [property: JsonPropertyName("engine")] string Engine);

internal sealed record BrowserExtensionStatResponse(
    [property: JsonPropertyName("downloadSpeed")] string DownloadSpeed,
    [property: JsonPropertyName("uploadSpeed")] string UploadSpeed,
    [property: JsonPropertyName("numActive")] string NumActive,
    [property: JsonPropertyName("numWaiting")] string NumWaiting,
    [property: JsonPropertyName("numStopped")] string NumStopped,
    [property: JsonPropertyName("numStoppedTotal")] string NumStoppedTotal);

internal sealed record BrowserExtensionActionResponse(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("error")] string? Error);
