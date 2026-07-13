namespace OmniDown.Models;

using System.ComponentModel;
using System.Runtime.CompilerServices;

public sealed class TorrentFileEntry : INotifyPropertyChanged
{
    private bool _isSelected = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Index { get; init; }

    public string Path { get; init; } = string.Empty;

    public long Length { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }

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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
