namespace OmniDown.Services.Downloads;

using System;
using System.Collections.Generic;
using System.Linq;

internal static class DownloadSourceParser
{
    public static List<string> ParseLines(string? text)
    {
        return (text ?? string.Empty)
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static bool IsLikelyDownloadSourceUri(string text)
    {
        if (text.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals(Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals("sftp", StringComparison.OrdinalIgnoreCase);
    }
}
