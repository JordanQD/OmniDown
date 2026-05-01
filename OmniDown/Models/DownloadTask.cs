namespace OmniDown.Models;

public sealed class DownloadTask
{
    public string Name { get; set; } = string.Empty;

    public string SourceUri { get; set; } = string.Empty;

    public string SaveDirectory { get; set; } = string.Empty;

    public string Status { get; set; } = "Waiting";

    public double Progress { get; set; }
}
