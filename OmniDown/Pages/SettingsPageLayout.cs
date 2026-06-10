using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace OmniDown.Pages;

internal static class SettingsPageLayout
{
    internal static ScrollViewer CreateSectionScrollViewer(UIElement content)
    {
        Grid host = new()
        {
            MaxWidth = 1064,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        if (content is FrameworkElement element)
        {
            element.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        host.Children.Add(content);

        ScrollViewer scrollViewer = new()
        {
            Padding = new Thickness(36, 0, 36, 0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = host
        };

        scrollViewer.SizeChanged += (_, args) =>
        {
            host.Width = Math.Min(1064, Math.Max(0, args.NewSize.Width - 72));
        };

        return scrollViewer;
    }
}
