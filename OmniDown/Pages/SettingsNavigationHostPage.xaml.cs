using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace OmniDown.Pages;

public sealed partial class SettingsNavigationHostPage : Page
{
    public SettingsNavigationHostPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Content = e.Parameter as Page;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        Content = null;
        base.OnNavigatedFrom(e);
    }
}
