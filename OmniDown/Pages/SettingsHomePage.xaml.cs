using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace OmniDown.Pages;

public sealed partial class SettingsHomePage : Page
{
    /// <summary>
    /// Fired when the user clicks a settings section card.
    /// The string argument is the section tag (e.g. "General", "Download").
    /// </summary>
    public event EventHandler<string>? SectionRequested;

    public SettingsHomePage()
    {
        InitializeComponent();
    }

    private void SettingsCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is SettingsCard card && card.Tag?.ToString() is string tag)
        {
            SectionRequested?.Invoke(this, tag);
        }
    }

    private void SettingsHomeScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SettingsHomeContent.Width = Math.Min(1064, Math.Max(0, e.NewSize.Width - 72));
    }
}
