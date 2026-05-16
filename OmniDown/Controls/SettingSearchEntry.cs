using Microsoft.UI.Xaml;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace OmniDown.Controls;

internal sealed class SettingSearchEntry(FrameworkElement element, params string[] searchableText)
{
    public FrameworkElement Element { get; } = element;

    public string[] SearchableText { get; } = searchableText;

    public void ApplyFilter(string query)
    {
        Element.Visibility = IsMatch(query) ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool IsMatch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        string normalizedQuery = NormalizeSearchText(query);
        return SearchableText
            .Select(NormalizeSearchText)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Any(text => text.Contains(normalizedQuery, StringComparison.Ordinal));
    }

    private static string NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        StringBuilder builder = new(value.Length);
        foreach (char character in value.Normalize(NormalizationForm.FormKC))
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category is UnicodeCategory.LowercaseLetter
                or UnicodeCategory.UppercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
                or UnicodeCategory.DecimalDigitNumber
                or UnicodeCategory.LetterNumber
                or UnicodeCategory.OtherNumber)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }
}
