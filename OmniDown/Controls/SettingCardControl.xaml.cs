using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OmniDown.Controls;

public sealed partial class SettingCardControl : UserControl
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(string), typeof(SettingCardControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(SettingCardControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(nameof(IconGlyph), typeof(string), typeof(SettingCardControl), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ActionContentProperty =
        DependencyProperty.Register(nameof(ActionContent), typeof(object), typeof(SettingCardControl), new PropertyMetadata(null));

    private const double WrapThreshold = 476;
    private const double WrapNoIconThreshold = 286;

    public SettingCardControl()
    {
        InitializeComponent();
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    private void SettingCardControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        if (width <= WrapNoIconThreshold)
            VisualStateManager.GoToState(this, "NarrowNoIcon", true);
        else if (width <= WrapThreshold)
            VisualStateManager.GoToState(this, "Narrow", true);
        else
            VisualStateManager.GoToState(this, "Wide", true);
    }
}
