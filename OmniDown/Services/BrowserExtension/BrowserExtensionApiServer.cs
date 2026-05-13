namespace OmniDown.Services.BrowserExtension;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal sealed class BrowserExtensionApiServer : IDisposable
{
    private const int MaxRequestBodyChars = 1024 * 1024;
    private readonly Func<BrowserExtensionDownloadRequest, Task<BrowserExtensionDownloadResult>> _downloadHandler;
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellation;
    private Task? _acceptTask;
    private int _port;
    private string _secret = string.Empty;

    public BrowserExtensionApiServer(Func<BrowserExtensionDownloadRequest, Task<BrowserExtensionDownloadResult>> downloadHandler)
    {
        _downloadHandler = downloadHandler;
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

        try
        {
            string? requestLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                await WriteJsonAsync(stream, 400, new { ok = false, error = "empty_request" }, cancellationToken);
                return;
            }

            string[] requestParts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (requestParts.Length < 2)
            {
                await WriteJsonAsync(stream, 400, new { ok = false, error = "invalid_request" }, cancellationToken);
                return;
            }

            string method = requestParts[0].Trim().ToUpperInvariant();
            string target = requestParts[1].Trim();
            Dictionary<string, string> headers = await ReadHeadersAsync(reader, cancellationToken);

            if (method == "OPTIONS")
            {
                await WriteJsonAsync(stream, 204, null, cancellationToken);
                return;
            }

            string body = await ReadBodyAsync(reader, headers, cancellationToken);
            BrowserExtensionHttpRequest request = BrowserExtensionHttpRequest.Parse(method, target, headers, body);

            if (request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(stream, 200, new { ok = true }, cancellationToken);
                return;
            }

            if (!IsDownloadEndpoint(request.Path) || method is not ("GET" or "POST"))
            {
                await WriteJsonAsync(stream, 404, new { ok = false, error = "not_found" }, cancellationToken);
                return;
            }

            if (!IsAuthorized(request.Secret))
            {
                await WriteJsonAsync(stream, 401, new { ok = false, error = "unauthorized" }, cancellationToken);
                return;
            }

            if (request.Urls.Count == 0)
            {
                await WriteJsonAsync(stream, 400, new { ok = false, error = "missing_url" }, cancellationToken);
                return;
            }

            BrowserExtensionDownloadResult result = await _downloadHandler(new BrowserExtensionDownloadRequest(request.Urls));
            await WriteJsonAsync(stream, result.Success ? 200 : 500, new
            {
                ok = result.Success,
                mode = result.Mode,
                count = result.Count,
                message = result.Message
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await WriteJsonAsync(stream, 500, new { ok = false, error = "server_error", message = ex.Message }, CancellationToken.None);
        }
        finally
        {
            client.Dispose();
        }
    }

    private bool IsAuthorized(string candidateSecret)
    {
        if (string.IsNullOrWhiteSpace(_secret) || string.IsNullOrWhiteSpace(candidateSecret))
        {
            return false;
        }

        byte[] expected = Encoding.UTF8.GetBytes(_secret);
        byte[] actual = Encoding.UTF8.GetBytes(candidateSecret.Trim());
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool IsDownloadEndpoint(string path)
    {
        return path.Equals("/api/download", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/download", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/capture", StringComparison.OrdinalIgnoreCase);
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

            string name = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();
            headers[name] = value;
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
        string header =
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "Access-Control-Allow-Methods: GET, POST, OPTIONS\r\n" +
            "Access-Control-Allow-Headers: Authorization, Content-Type, X-OmniDown-Secret\r\n" +
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

    private sealed record BrowserExtensionHttpRequest(
        string Method,
        string Path,
        IReadOnlyDictionary<string, string> Headers,
        IReadOnlyDictionary<string, string> Query,
        string Body,
        string Secret,
        IReadOnlyList<string> Urls)
    {
        public static BrowserExtensionHttpRequest Parse(
            string method,
            string target,
            IReadOnlyDictionary<string, string> headers,
            string body)
        {
            string path = target;
            string queryText = string.Empty;
            int queryStart = target.IndexOf('?');
            if (queryStart >= 0)
            {
                path = target[..queryStart];
                queryText = target[(queryStart + 1)..];
            }

            Dictionary<string, string> query = ParseQuery(queryText);
            List<string> urls = [];
            string secret = ResolveSecret(headers, query);

            AddValue(urls, GetQueryValue(query, "url"));
            AddValue(urls, GetQueryValue(query, "uri"));
            AddValue(urls, GetQueryValue(query, "text"));

            if (!string.IsNullOrWhiteSpace(body))
            {
                ParseBody(body, headers, urls, ref secret);
            }

            return new BrowserExtensionHttpRequest(method, path, headers, query, body, secret, urls);
        }

        private static void ParseBody(
            string body,
            IReadOnlyDictionary<string, string> headers,
            List<string> urls,
            ref string secret)
        {
            string contentType = headers.TryGetValue("Content-Type", out string? value) ? value : string.Empty;
            if (contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                ParseJsonBody(body, urls, ref secret);
                return;
            }

            if (contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<string, string> form = ParseQuery(body);
                AddValue(urls, GetQueryValue(form, "url"));
                AddValue(urls, GetQueryValue(form, "uri"));
                AddValue(urls, GetQueryValue(form, "text"));
                if (string.IsNullOrWhiteSpace(secret))
                {
                    secret = GetQueryValue(form, "secret") ?? GetQueryValue(form, "token") ?? string.Empty;
                }

                return;
            }

            AddValue(urls, body.Trim());
        }

        private static void ParseJsonBody(string body, List<string> urls, ref string secret)
        {
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            AddJsonString(urls, root, "url");
            AddJsonString(urls, root, "uri");
            AddJsonString(urls, root, "text");
            AddJsonArray(urls, root, "urls");
            AddJsonArray(urls, root, "uris");

            if (string.IsNullOrWhiteSpace(secret))
            {
                secret = GetJsonString(root, "secret") ?? GetJsonString(root, "token") ?? string.Empty;
            }
        }

        private static string ResolveSecret(
            IReadOnlyDictionary<string, string> headers,
            IReadOnlyDictionary<string, string> query)
        {
            if (headers.TryGetValue("X-OmniDown-Secret", out string? headerSecret) &&
                !string.IsNullOrWhiteSpace(headerSecret))
            {
                return headerSecret;
            }

            if (headers.TryGetValue("Authorization", out string? authorization) &&
                authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authorization["Bearer ".Length..];
            }

            return GetQueryValue(query, "secret") ?? GetQueryValue(query, "token") ?? string.Empty;
        }

        private static void AddJsonString(List<string> urls, JsonElement root, string propertyName)
        {
            string? value = GetJsonString(root, propertyName);
            AddValue(urls, value);
        }

        private static string? GetJsonString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out JsonElement property) &&
                property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : null;
        }

        private static void AddJsonArray(List<string> urls, JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement property))
            {
                return;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                AddValue(urls, property.GetString());
                return;
            }

            if (property.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (JsonElement item in property.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    AddValue(urls, item.GetString());
                }
            }
        }

        private static void AddValue(List<string> urls, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            foreach (string line in value.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) &&
                    !urls.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                {
                    urls.Add(trimmed);
                }
            }
        }

        private static Dictionary<string, string> ParseQuery(string queryText)
        {
            Dictionary<string, string> query = new(StringComparer.OrdinalIgnoreCase);
            foreach (string part in queryText.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] pair = part.Split('=', 2);
                string key = DecodeQueryValue(pair[0]);
                string value = pair.Length == 2 ? DecodeQueryValue(pair[1]) : string.Empty;
                query[key] = value;
            }

            return query;
        }

        private static string? GetQueryValue(IReadOnlyDictionary<string, string> query, string name)
        {
            return query.TryGetValue(name, out string? value) ? value : null;
        }

        private static string DecodeQueryValue(string value)
        {
            try
            {
                return Uri.UnescapeDataString(value.Replace('+', ' '));
            }
            catch
            {
                return value;
            }
        }
    }
}

internal sealed record BrowserExtensionDownloadRequest(IReadOnlyList<string> Urls);

internal sealed record BrowserExtensionDownloadResult(bool Success, string Mode, int Count, string Message);
