namespace OmniDown.Models;

public sealed class TorrentFileEntry
{
    public int Index { get; init; }

    public string Path { get; init; } = string.Empty;

    public long Length { get; init; }

    public bool IsSelected { get; set; } = true;

    public string SizeText => FormatBytes(Length);

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
