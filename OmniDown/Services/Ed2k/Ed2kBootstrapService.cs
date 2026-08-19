using OmniDown.Models.Settings;
using OmniDown.Services.Storage;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OmniDown.Services.Ed2k;

public sealed record Ed2kBootstrapStatus(
    long? ServerMetSize,
    long? NodesDatSize,
    DateTimeOffset? ServerMetModified,
    DateTimeOffset? NodesDatModified)
{
    public bool IsReady => ServerMetSize > 0 && NodesDatSize > 0;
}

public sealed class Ed2kBootstrapService : IDisposable
{
    private const long MaximumBootstrapFileSize = 16 * 1024 * 1024;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _syncGate = new(1, 1);

    public Ed2kBootstrapService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public Ed2kBootstrapStatus GetStatus()
    {
        return new Ed2kBootstrapStatus(
            GetLength(AppPaths.Ed2kServerMetPath),
            GetLength(AppPaths.Ed2kNodesDatPath),
            GetModified(AppPaths.Ed2kServerMetPath),
            GetModified(AppPaths.Ed2kNodesDatPath));
    }

    public async Task<Ed2kBootstrapStatus> EnsureAvailableAsync(
        Ed2kSettings settings,
        CancellationToken cancellationToken = default)
    {
        Ed2kBootstrapStatus status = GetStatus();
        if (status.IsReady)
        {
            return status;
        }

        return await SyncAsync(settings.ServerListUrl, settings.KadBootstrapUrl, cancellationToken);
    }

    public async Task<Ed2kBootstrapStatus> SyncAsync(
        string serverMetUrl,
        string nodesDatUrl,
        CancellationToken cancellationToken = default)
    {
        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            Uri serverUri = ValidateHttpUri(serverMetUrl, "server.met");
            Uri nodesUri = ValidateHttpUri(nodesDatUrl, "nodes.dat");
            byte[] serverBytes = await DownloadFileAsync(serverUri, cancellationToken);
            byte[] nodesBytes = await DownloadFileAsync(nodesUri, cancellationToken);

            Directory.CreateDirectory(AppPaths.Ed2kBootstrapDirectory);
            WriteAtomically(AppPaths.Ed2kServerMetPath, serverBytes);
            WriteAtomically(AppPaths.Ed2kNodesDatPath, nodesBytes);
            return GetStatus();
        }
        finally
        {
            _syncGate.Release();
        }
    }

    public static bool IsSyncDue(Ed2kSettings settings, DateTimeOffset now)
    {
        if (!settings.AutoSyncEnabled)
        {
            return false;
        }

        if (settings.LastSyncTime <= 0)
        {
            return true;
        }

        TimeSpan interval = settings.SyncInterval switch
        {
            "EveryStart" => TimeSpan.Zero,
            "Every6Hours" => TimeSpan.FromHours(6),
            "Every12Hours" => TimeSpan.FromHours(12),
            "Weekly" => TimeSpan.FromDays(7),
            _ => TimeSpan.FromDays(1)
        };
        DateTimeOffset lastSync = DateTimeOffset.FromUnixTimeMilliseconds(settings.LastSyncTime);
        return now - lastSync >= interval;
    }

    private async Task<byte[]> DownloadFileAsync(Uri uri, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await _httpClient.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength is long length &&
            (length <= 0 || length > MaximumBootstrapFileSize))
        {
            throw new InvalidDataException($"Invalid ED2K bootstrap file size: {length}.");
        }

        await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
        using MemoryStream target = new();
        byte[] buffer = new byte[81920];
        while (true)
        {
            int read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (target.Length + read > MaximumBootstrapFileSize)
            {
                throw new InvalidDataException("ED2K bootstrap file exceeds 16 MiB.");
            }

            target.Write(buffer, 0, read);
        }

        if (target.Length == 0)
        {
            throw new InvalidDataException("ED2K bootstrap file is empty.");
        }

        return target.ToArray();
    }

    private static Uri ValidateHttpUri(string value, string fileName)
    {
        if (!Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new FormatException($"{fileName} source must be an HTTP or HTTPS URL.");
        }

        return uri;
    }

    private static void WriteAtomically(string path, byte[] bytes)
    {
        string temporaryPath = path + ".tmp";
        try
        {
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static long? GetLength(string path) =>
        File.Exists(path) ? new FileInfo(path).Length : null;

    private static DateTimeOffset? GetModified(string path) =>
        File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;

    public void Dispose() => _httpClient.Dispose();
}
