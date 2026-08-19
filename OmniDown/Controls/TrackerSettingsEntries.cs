using Microsoft.UI.Xaml;

namespace OmniDown.Controls;

public sealed class TrackerSourceEntry
{
    public string DisplayName { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public bool IsSelected { get; set; }

    public Visibility ModifyVisibility { get; set; } = Visibility.Visible;
}

public sealed class TrackerEntry
{
    public string Address { get; set; } = string.Empty;

    public bool IsSelected { get; set; } = true;
}
