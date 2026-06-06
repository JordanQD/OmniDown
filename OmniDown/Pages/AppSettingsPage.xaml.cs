using Microsoft.UI.Xaml.Controls;
using OmniDown.Controls;

namespace OmniDown.Pages;

public sealed partial class AppSettingsPage : Page
{
    public SettingsPageControl SettingsPageControl => _settingsPageControl;

    public AppSettingsPage()
    {
        InitializeComponent();
    }
}
