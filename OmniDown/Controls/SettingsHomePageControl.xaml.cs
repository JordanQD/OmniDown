using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace OmniDown.Controls;

public sealed partial class SettingsHomePageControl : UserControl
{
    public event EventHandler<string>? SectionRequested;

    public SettingsHomePageControl()
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
}
