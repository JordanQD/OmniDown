namespace OmniDown.Dialogs;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models.Settings;
using OmniDown.Services.Downloads;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

internal static class NewDownloadDialogHelpers
{
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
            string[] sourceUris = DownloadSourceParser.ParseLines(clipboardText)
                .Select(uri => NormalizeClipboardSourceUri(uri, settings))
                .Where(uri => uri is not null)
                .Select(uri => uri!)
                .ToArray();
            return sourceUris.Length == 0
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
            int selectionLength = Math.Clamp(
                textBox.SelectionLength,
                0,
                currentText.Length - selectionStart);
            string prefix = currentText[..selectionStart];
            string suffix = currentText[(selectionStart + selectionLength)..];

            if (!string.IsNullOrWhiteSpace(prefix) && !EndsWithLineBreak(prefix))
            {
                clipboardText = Environment.NewLine + clipboardText;
            }

            textBox.Text = prefix + clipboardText + suffix;
            textBox.SelectionStart = (prefix + clipboardText).Length;
            textBox.SelectionLength = 0;
            _ = textBox.Focus(FocusState.Programmatic);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Paste download URL failed: {ex}");
        }
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
