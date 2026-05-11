namespace OmniDown.ViewModels;

using Microsoft.UI.Xaml.Media;

internal sealed record AppStatusMessage(
    string Message,
    string DetailText,
    string SeverityText,
    string SeverityGlyph,
    Brush SeverityBrush);
