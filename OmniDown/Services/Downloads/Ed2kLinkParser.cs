using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace OmniDown.Services.Downloads;

public sealed record Ed2kFileLink(
    string OriginalLink,
    string DisplayName,
    long FileSize,
    string FileHash);

public static class Ed2kLinkParser
{
    private const string FilePrefix = "ed2k://|file|";

    public static bool IsEd2kLink(string? value) =>
        value?.TrimStart().StartsWith("ed2k://", StringComparison.OrdinalIgnoreCase) == true;

    public static bool TryParseFileLink(string? value, out Ed2kFileLink? link)
    {
        link = null;
        string text = value?.Trim() ?? string.Empty;
        if (!text.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string[] parts = text.Split('|');
        if (parts.Length < 6 ||
            !parts[1].Equals("file", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(parts[2]) ||
            !long.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out long fileSize) ||
            fileSize < 0 ||
            !IsEd2kHash(parts[4]) ||
            !parts[^1].Equals("/", StringComparison.Ordinal))
        {
            return false;
        }

        string displayName = SanitizeDisplayName(DecodeComponent(parts[2]));
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return false;
        }

        link = new Ed2kFileLink(text, displayName, fileSize, parts[4].ToLowerInvariant());
        return true;
    }

    public static Ed2kFileLink ParseFileLink(string value)
    {
        if (TryParseFileLink(value, out Ed2kFileLink? link))
        {
            return link!;
        }

        throw new FormatException("ED2K file link is invalid.");
    }

    private static bool IsEd2kHash(string value) =>
        value.Length == 32 && value.All(Uri.IsHexDigit);

    private static string DecodeComponent(string value)
    {
        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch (UriFormatException)
        {
            return value;
        }
    }

    private static string SanitizeDisplayName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string sanitized = new(value
            .Select(character => invalid.Contains(character) || character is '/' or '\\' ? '_' : character)
            .ToArray());
        sanitized = sanitized.Trim().TrimEnd('.', ' ');
        return sanitized.Length <= 255 ? sanitized : sanitized[..255].TrimEnd('.', ' ');
    }
}
