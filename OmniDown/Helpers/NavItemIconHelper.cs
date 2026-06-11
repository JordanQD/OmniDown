using Microsoft.UI.Xaml;

namespace OmniDown.Helpers;

internal class NavItemIconHelper
{
    public static object GetSelectedIcon(DependencyObject obj) =>
        obj.GetValue(SelectedIconProperty);

    public static void SetSelectedIcon(DependencyObject obj, object value) =>
        obj.SetValue(SelectedIconProperty, value);

    public static readonly DependencyProperty SelectedIconProperty =
        DependencyProperty.RegisterAttached(
            "SelectedIcon", typeof(object), typeof(NavItemIconHelper),
            new PropertyMetadata(null));

    public static object GetUnselectedIcon(DependencyObject obj) =>
        obj.GetValue(UnselectedIconProperty);

    public static void SetUnselectedIcon(DependencyObject obj, object value) =>
        obj.SetValue(UnselectedIconProperty, value);

    public static readonly DependencyProperty UnselectedIconProperty =
        DependencyProperty.RegisterAttached(
            "UnselectedIcon", typeof(object), typeof(NavItemIconHelper),
            new PropertyMetadata(null));

    public static Visibility GetStaticIconVisibility(DependencyObject obj) =>
        (Visibility)obj.GetValue(StaticIconVisibilityProperty);

    public static void SetStaticIconVisibility(DependencyObject obj, Visibility value) =>
        obj.SetValue(StaticIconVisibilityProperty, value);

    public static readonly DependencyProperty StaticIconVisibilityProperty =
        DependencyProperty.RegisterAttached(
            "StaticIconVisibility", typeof(Visibility), typeof(NavItemIconHelper),
            new PropertyMetadata(Visibility.Collapsed));

    public static bool GetShowNotificationDot(DependencyObject obj) =>
        (bool)obj.GetValue(ShowNotificationDotProperty);

    public static void SetShowNotificationDot(DependencyObject obj, bool value) =>
        obj.SetValue(ShowNotificationDotProperty, value);

    public static readonly DependencyProperty ShowNotificationDotProperty =
        DependencyProperty.RegisterAttached(
            "ShowNotificationDot", typeof(bool), typeof(NavItemIconHelper),
            new PropertyMetadata(false));
}
