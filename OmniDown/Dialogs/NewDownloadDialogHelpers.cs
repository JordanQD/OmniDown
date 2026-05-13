namespace OmniDown.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using OmniDown.Models;
using OmniDown.Models.Settings;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI;

internal static class NewDownloadDialogHelpers
{
    public static TextBlock CreateHeaderText(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
    }

    public static Button CreateTaskTypeButton(string text)
    {
        return new Button
        {
            Content = text,
            Background = new SolidColorBrush(Colors.Transparent),
            BorderBrush = new SolidColorBrush(Colors.Transparent),
            Padding = new Thickness(16, 8, 16, 8),
            MinWidth = 96,
            HorizontalAlignment = HorizontalAlignment.Left
        };
    }

    public static Grid CreateTorrentInputRow(
        TextBlock fileNameText,
        Border torrentTag,
        Button openButton,
        Button clearButton)
    {
        Grid displayContent = new()
        {
            Height = 32,
            Padding = new Thickness(12, 0, 6, 0),
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children =
            {
                fileNameText,
                torrentTag
            }
        };
        Grid.SetColumn(torrentTag, 1);
        Border displayBorder = new()
        {
            Background = GetThemeBrush("CardBackgroundFillColorSecondaryBrush", Colors.Transparent),
            CornerRadius = new CornerRadius(6),
            Child = displayContent
        };

        Grid row = new()
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            Children =
            {
                displayBorder,
                openButton,
                clearButton
            }
        };
        Grid.SetColumn(openButton, 1);
        Grid.SetColumn(clearButton, 1);
        openButton.Margin = new Thickness(8, 0, 0, 0);
        clearButton.Margin = new Thickness(8, 0, 0, 0);
        return row;
    }

    public static Border CreateTorrentTag()
    {
        return new Border
        {
            Background = GetThemeBrush("AccentFillColorDefaultBrush", Colors.DodgerBlue),
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(8, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "Torrent",
                FontSize = 12,
                Foreground = GetThemeBrush("TextOnAccentFillColorPrimaryBrush", Colors.White)
            }
        };
    }

    public static Button CreateIconButton(string glyph, string tooltip)
    {
        Button button = new()
        {
            Content = new FontIcon
            {
                Glyph = glyph,
                FontSize = 16,
                Width = 16,
                Height = 16
            },
            Width = 40,
            Height = 32,
            MinWidth = 40,
            MinHeight = 32,
            Padding = new Thickness(0),
            VerticalAlignment = VerticalAlignment.Center
        };
        ToolTipService.SetToolTip(button, tooltip);
        return button;
    }

    public static Border CreateTaskTypeIndicator()
    {
        return new Border
        {
            Height = 3,
            Margin = new Thickness(12, 0, 12, 0),
            Background = GetThemeBrush("AccentFillColorDefaultBrush", Colors.DodgerBlue),
            CornerRadius = new CornerRadius(2),
            VerticalAlignment = VerticalAlignment.Bottom
        };
    }

    public static Brush GetThemeBrush(string key, Color fallback)
    {
        return Application.Current.Resources.TryGetValue(key, out object value) && value is Brush brush
            ? brush
            : new SolidColorBrush(fallback);
    }

    public static void RenderTorrentRows(
        StackPanel rowsPanel,
        IEnumerable<TorrentFileEntry> files,
        CheckBox selectAllCheckBox)
    {
        rowsPanel.Children.Clear();
        List<TorrentFileEntry> fileList = files.ToList();
        selectAllCheckBox.IsChecked = fileList.Count == 0
            ? false
            : fileList.All(file => file.IsSelected)
                ? true
                : fileList.Any(file => file.IsSelected)
                    ? null
                    : false;

        foreach (TorrentFileEntry file in fileList)
        {
            CheckBox checkBox = new()
            {
                IsChecked = file.IsSelected,
                VerticalAlignment = VerticalAlignment.Center
            };
            checkBox.Click += (_, _) =>
            {
                file.IsSelected = checkBox.IsChecked == true;
                RenderTorrentRows(rowsPanel, fileList, selectAllCheckBox);
            };

            Grid row = new()
            {
                MinHeight = 38,
                Padding = new Thickness(8, 0, 8, 0),
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(40) },
                    new ColumnDefinition { Width = new GridLength(54) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(96) }
                },
                Children =
                {
                    checkBox,
                    new TextBlock
                    {
                        Text = file.Index.ToString(CultureInfo.InvariantCulture),
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = file.Path,
                        TextTrimming = TextTrimming.CharacterEllipsis,
                        VerticalAlignment = VerticalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = file.SizeText,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            };
            Grid.SetColumn((FrameworkElement)row.Children[1], 1);
            Grid.SetColumn((FrameworkElement)row.Children[2], 2);
            Grid.SetColumn((FrameworkElement)row.Children[3], 3);
            rowsPanel.Children.Add(new Border
            {
                BorderBrush = GetThemeBrush("ControlStrokeColorDefaultBrush", Colors.Gray),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Child = row
            });
        }
    }

    public static void UpdateUriTextBoxHeight(TextBox textBox)
    {
        const double singleLineHeight = 32;
        const double lineHeight = 22;
        const double maxHeight = 220;
        const double horizontalPadding = 28;
        const double averageCharacterWidth = 7.4;

        double textWidth = textBox.ActualWidth > 0
            ? Math.Max(160, textBox.ActualWidth - horizontalPadding)
            : 560;
        int charactersPerLine = Math.Max(20, (int)Math.Floor(textWidth / averageCharacterWidth));
        int visualLineCount = 0;

        string normalizedText = (textBox.Text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        foreach (string line in normalizedText.Split('\n'))
        {
            visualLineCount += Math.Max(1, (int)Math.Ceiling(Math.Max(1, line.Length) / (double)charactersPerLine));
        }

        double desiredHeight = singleLineHeight + (Math.Max(1, visualLineCount) - 1) * lineHeight;
        textBox.Height = Math.Clamp(desiredHeight, singleLineHeight, maxHeight);
    }

    public static List<string> GetDownloadSourceUris(string text)
    {
        return text
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static async Task<string?> GetClipboardDownloadTextAsync(AdvancedSettings? settings = null)
    {
        try
        {
            DataPackageView content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text))
            {
                return null;
            }

            string clipboardText = await content.GetTextAsync();
            List<string> sourceUris = GetDownloadSourceUris(clipboardText)
                .Select(uri => NormalizeClipboardSourceUri(uri, settings))
                .Where(uri => uri is not null)
                .Select(uri => uri!)
                .ToList();
            return sourceUris.Count == 0
                ? null
                : EnsureTrailingLineBreak(string.Join(Environment.NewLine, sourceUris));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Read clipboard download URL failed: {ex}");
            return null;
        }
    }

    public static async Task PasteClipboardTextAsync(TextBox textBox)
    {
        try
        {
            DataPackageView content = Clipboard.GetContent();
            if (!content.Contains(StandardDataFormats.Text))
            {
                return;
            }

            string clipboardText = EnsureTrailingLineBreak((await content.GetTextAsync()).Trim());
            if (string.IsNullOrWhiteSpace(clipboardText))
            {
                return;
            }

            string currentText = textBox.Text ?? string.Empty;
            int selectionStart = Math.Clamp(textBox.SelectionStart, 0, currentText.Length);
            int selectionLength = Math.Clamp(textBox.SelectionLength, 0, currentText.Length - selectionStart);
            string prefix = currentText[..selectionStart];
            string suffix = currentText[(selectionStart + selectionLength)..];

            if (!string.IsNullOrWhiteSpace(prefix) && !EndsWithLineBreak(prefix))
            {
                clipboardText = Environment.NewLine + clipboardText;
            }

            textBox.Text = prefix + clipboardText + suffix;
            textBox.SelectionStart = (prefix + clipboardText).Length;
            textBox.SelectionLength = 0;
            textBox.Focus(FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Paste download URL failed: {ex}");
        }
    }

    public static int GetDownloadSplitCount(NumberBox numberBox)
    {
        if (double.IsNaN(numberBox.Value))
        {
            return 1;
        }

        return Math.Clamp((int)Math.Round(numberBox.Value), 1, 256);
    }

    public static bool IsLikelyDownloadSourceUri(string text)
    {
        if (text.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals(Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals("sftp", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeClipboardSourceUri(string text, AdvancedSettings? settings)
    {
        bool httpEnabled = settings?.ClipboardHttpEnabled ?? true;
        bool ftpEnabled = settings?.ClipboardFtpEnabled ?? true;
        bool magnetEnabled = settings?.ClipboardMagnetEnabled ?? true;
        bool thunderEnabled = settings?.ClipboardThunderEnabled ?? false;
        bool btHashEnabled = settings?.ClipboardBtHashEnabled ?? false;

        if (text.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
        {
            return magnetEnabled ? text : null;
        }

        if (text.StartsWith("thunder://", StringComparison.OrdinalIgnoreCase))
        {
            return thunderEnabled ? text : null;
        }

        if (btHashEnabled && IsLikelyBtHash(text))
        {
            return $"magnet:?xt=urn:btih:{text}";
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out Uri? uri))
        {
            return null;
        }

        if ((uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) &&
            httpEnabled)
        {
            return text;
        }

        if ((uri.Scheme.Equals(Uri.UriSchemeFtp, StringComparison.OrdinalIgnoreCase) ||
                uri.Scheme.Equals("sftp", StringComparison.OrdinalIgnoreCase)) &&
            ftpEnabled)
        {
            return text;
        }

        return null;
    }

    private static bool IsLikelyBtHash(string text)
    {
        string trimmed = text.Trim();
        return trimmed.Length is 32 or 40 &&
            trimmed.All(character =>
                (character >= '0' && character <= '9') ||
                (character >= 'a' && character <= 'f') ||
                (character >= 'A' && character <= 'F'));
    }

    private static string EnsureTrailingLineBreak(string text)
    {
        return string.IsNullOrEmpty(text) || EndsWithLineBreak(text)
            ? text
            : text + Environment.NewLine;
    }

    private static bool EndsWithLineBreak(string text)
    {
        return text.EndsWith('\r') || text.EndsWith('\n');
    }
}
