using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace WinUIGallery
{
    public static class App
    {
        public static GalleryMainWindowAdapter MainWindow { get; } = new();
    }

    public sealed class GalleryMainWindowAdapter
    {
        public NavigationView NavigationView { get; set; } = new();

        public void Navigate(Type pageType, object? parameter = null)
        {
        }
    }
}

namespace WinUIGallery.Pages
{
    public sealed class ItemPage : Page
    {
    }
}

namespace WinUIGallery.Helpers
{
    public static class ProcessInfoHelper
    {
        public static Version? GetVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
    }

    public static class VersionHelper
    {
        public static string WinAppSdkRuntimeDetails => "Windows App SDK";
    }

    public sealed class SettingsHelper
    {
        private static readonly SettingsHelper Instance = new();

        public static SettingsHelper Current => Instance;

        public List<string> RecentlyVisited { get; } = [];

        public List<string> Favorites { get; } = [];

        public void UpdateRecentlyVisited(Action<List<string>> update)
        {
            update(RecentlyVisited);
        }

        public void UpdateFavorites(Action<List<string>> update)
        {
            update(Favorites);
        }
    }

    public static class ThemeHelper
    {
        private static ElementTheme rootTheme = ElementTheme.Default;

        public static ElementTheme RootTheme
        {
            get => rootTheme;
            set
            {
                rootTheme = value;
                if (WindowHelper.LastRootElement is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = value;
                }
            }
        }

        public static ElementTheme ActualTheme => WindowHelper.LastRootElement?.ActualTheme ?? ElementTheme.Default;
    }

    public static class EnumHelper
    {
        public static T GetEnum<T>(string text)
            where T : struct, Enum
        {
            return Enum.TryParse(text, ignoreCase: true, out T value) ? value : default;
        }
    }

    public static class WindowHelper
    {
        internal static FrameworkElement? LastRootElement { get; private set; }

        public static Window? GetWindowForElement(UIElement element)
        {
            LastRootElement = element.XamlRoot?.Content as FrameworkElement;
            return null;
        }
    }

    public static class TitleBarHelper
    {
        public static void ApplySystemThemeToCaptionButtons(Window window, ElementTheme theme)
        {
        }
    }

    public static class UIHelper
    {
        public static void AnnounceActionForAccessibility(UIElement element, string announcement, string activityId)
        {
            FrameworkElementAutomationPeer.FromElement(element)?.RaiseNotificationEvent(
                AutomationNotificationKind.ActionCompleted,
                AutomationNotificationProcessing.ImportantMostRecent,
                announcement,
                activityId);
        }
    }

    public static class NavigationOrientationHelper
    {
        public static void IsLeftModeForElement(bool isLeftMode)
        {
        }
    }
}
