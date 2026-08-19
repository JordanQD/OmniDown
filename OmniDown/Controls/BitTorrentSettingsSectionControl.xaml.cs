using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OmniDown.Controls;

public sealed partial class BitTorrentSettingsSectionControl : UserControl
{
    private static readonly (string Label, string Url)[] BuiltInTrackerSources =
    [
        ("ngosang · trackers_best.txt", "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_best.txt"),
        ("ngosang · trackers_best_ip.txt", "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_best_ip.txt"),
        ("ngosang · trackers_all.txt", "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_all.txt"),
        ("ngosang · trackers_all_ip.txt", "https://raw.githubusercontent.com/ngosang/trackerslist/master/trackers_all_ip.txt"),
        ("ngosang · trackers_best.txt CDN", "https://cdn.jsdelivr.net/gh/ngosang/trackerslist/trackers_best.txt"),
        ("ngosang · trackers_best_ip.txt CDN", "https://cdn.jsdelivr.net/gh/ngosang/trackerslist/trackers_best_ip.txt"),
        ("ngosang · trackers_all.txt CDN", "https://cdn.jsdelivr.net/gh/ngosang/trackerslist/trackers_all.txt"),
        ("ngosang · trackers_all_ip.txt CDN", "https://cdn.jsdelivr.net/gh/ngosang/trackerslist/trackers_all_ip.txt"),
        ("XIU2 · best.txt", "https://raw.githubusercontent.com/XIU2/TrackersListCollection/master/best.txt"),
        ("XIU2 · all.txt", "https://raw.githubusercontent.com/XIU2/TrackersListCollection/master/all.txt"),
        ("XIU2 · http.txt", "https://raw.githubusercontent.com/XIU2/TrackersListCollection/master/http.txt"),
        ("XIU2 · best.txt CDN", "https://cdn.jsdelivr.net/gh/XIU2/TrackersListCollection/best.txt"),
        ("XIU2 · all.txt CDN", "https://cdn.jsdelivr.net/gh/XIU2/TrackersListCollection/all.txt"),
        ("XIU2 · http.txt CDN", "https://cdn.jsdelivr.net/gh/XIU2/TrackersListCollection/http.txt")
    ];

    public ObservableCollection<TrackerSourceEntry> TrackerSourceEntries { get; } = [];

    public ObservableCollection<TrackerEntry> TrackerEntries { get; } = [];

    public BitTorrentSettingsSectionControl()
    {
        InitializeComponent();
        UpdateTrackerEmptyStates();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(BtAutoDownloadSettingCard, "bittorrent", "torrent", "metalink", "magnet", "auto", "内容", "自动"),
        new(BtForceEncryptionSettingCard, "bittorrent", "encryption", "crypto", "加密"),
        new(BtKeepSeedingSettingCard, "bittorrent", "seed", "keep", "ratio", "time", "bt", "做种", "分享率", "时间"),
        new(BtMaxPeersSettingCard, "bittorrent", "peer", "max", "bt", "连接"),
        new(BtTrackerSourceSettingCard, "bittorrent", "tracker", "source", "custom", "url", "sync", "bt", "来源", "自定义", "同步"),
        new(BtTrackerListSettingCard, "bittorrent", "tracker", "list", "bt", "列表"),
        new(BtAutoSyncTrackerSettingCard, "bittorrent", "tracker", "auto sync", "bt", "自动同步")
    ];

    internal StackPanel BitTorrentSettingsContentControl => BitTorrentSettingsContent;
    internal ToggleSwitch BtAutoDownloadToggleSwitchControl => BtAutoDownloadToggleSwitch;
    internal TextBlock BtAutoDownloadStateTextControl => BtAutoDownloadStateText;
    internal ToggleSwitch BtForceEncryptionToggleSwitchControl => BtForceEncryptionToggleSwitch;
    internal TextBlock BtForceEncryptionStateTextControl => BtForceEncryptionStateText;
    internal ComboBox BtSeedingModeComboBoxControl => BtSeedingModeComboBox;
    internal NumberBox BtSeedRatioNumberBoxControl => BtSeedRatioNumberBox;
    internal NumberBox BtSeedTimeNumberBoxControl => BtSeedTimeNumberBox;
    internal NumberBox BtMaxPeersNumberBoxControl => BtMaxPeersNumberBox;
    internal Button BtSyncTrackerButtonControl => BtSyncTrackerButton;
    internal ToggleSwitch BtAutoSyncTrackerToggleSwitchControl => BtAutoSyncTrackerToggleSwitch;
    internal TextBlock BtAutoSyncTrackerStateTextControl => BtAutoSyncTrackerStateText;
    internal TextBlock BtLastTrackerSyncTextControl => BtLastTrackerSyncText;
    internal IReadOnlyList<string> SelectedTrackerSourceUrls => TrackerSourceEntries
        .Where(entry => entry.IsSelected)
        .Select(entry => entry.Url)
        .ToArray();
    internal IReadOnlyList<string> CustomTrackerSourceUrls => TrackerSourceEntries
        .Select(entry => entry.Url)
        .Where(url => !IsBuiltInTrackerSource(url))
        .ToArray();
    internal IReadOnlyList<string> TrackerAddresses => TrackerEntries.Select(entry => entry.Address).ToArray();
    internal IReadOnlyList<string> DisabledTrackerAddresses => TrackerEntries
        .Where(entry => !entry.IsSelected)
        .Select(entry => entry.Address)
        .ToArray();

    internal event RoutedEventHandler? BitTorrentSettingChanged;
    internal event RoutedEventHandler? SyncBtTrackerRequested;

    internal void SetTrackerSources(IEnumerable<string> selectedUrls, IEnumerable<string> customUrls)
    {
        HashSet<string> selected = NormalizeHttpUrls(selectedUrls).ToHashSet(StringComparer.OrdinalIgnoreCase);
        TrackerSourceEntries.Clear();
        foreach (string url in NormalizeHttpUrls(
            BuiltInTrackerSources.Select(source => source.Url).Concat(customUrls ?? [])))
        {
            TrackerSourceEntries.Add(CreateTrackerSourceEntry(url, selected.Contains(url)));
        }

        UpdateTrackerEmptyStates();
    }

    internal void SetTrackerAddresses(IEnumerable<string> addresses, IEnumerable<string>? disabledAddresses = null)
    {
        HashSet<string> disabled = disabledAddresses?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        TrackerEntries.Clear();
        foreach (string address in addresses
            .Select(NormalizeValue)
            .Where(IsValidTrackerAddress)
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            TrackerEntries.Add(new TrackerEntry
            {
                Address = address,
                IsSelected = !disabled.Contains(address)
            });
        }

        UpdateTrackerEmptyStates();
    }

    internal void SetTrackerControlsEnabled(bool isEnabled)
    {
        BtTrackerSourceSettingCard.IsEnabled = isEnabled;
        BtTrackerListSettingCard.IsEnabled = isEnabled;
        BtSyncTrackerButton.IsEnabled = isEnabled;
    }

    private async void AddBtTrackerSourceButton_Click(object sender, RoutedEventArgs args)
    {
        string? url = await ShowTrackerSourceEditorAsync(null);
        if (url is null)
        {
            return;
        }

        TrackerSourceEntries.Add(CreateTrackerSourceEntry(url, true));
        NotifyTrackerCollectionChanged();
    }

    private async void EditBtTrackerSourceButton_Click(object sender, RoutedEventArgs args)
    {
        string originalUrl = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        int index = FindTrackerSourceIndex(originalUrl);
        if (index < 0)
        {
            return;
        }

        string? url = await ShowTrackerSourceEditorAsync(originalUrl);
        if (url is null || url.Equals(originalUrl, StringComparison.Ordinal))
        {
            return;
        }

        TrackerSourceEntries[index] = CreateTrackerSourceEntry(url, TrackerSourceEntries[index].IsSelected);
        NotifyTrackerCollectionChanged();
    }

    private void DeleteBtTrackerSourceButton_Click(object sender, RoutedEventArgs args)
    {
        string url = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        int index = FindTrackerSourceIndex(url);
        if (index < 0)
        {
            return;
        }

        TrackerSourceEntries.RemoveAt(index);
        NotifyTrackerCollectionChanged();
    }

    private void BtTrackerSourceEnabledToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        string url = toggleSwitch.Tag?.ToString() ?? string.Empty;
        int index = FindTrackerSourceIndex(url);
        if (index < 0 || TrackerSourceEntries[index].IsSelected == toggleSwitch.IsOn)
        {
            return;
        }

        TrackerSourceEntries[index].IsSelected = toggleSwitch.IsOn;
        BitTorrentSettingChanged?.Invoke(this, new RoutedEventArgs());
    }

    private async void AddBtTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        string? address = await ShowTrackerEditorAsync(null);
        if (address is null)
        {
            return;
        }

        TrackerEntries.Add(new TrackerEntry { Address = address, IsSelected = true });
        NotifyTrackerCollectionChanged();
    }

    private async void EditBtTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        string originalAddress = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        int index = FindTrackerIndex(originalAddress);
        if (index < 0)
        {
            return;
        }

        string? address = await ShowTrackerEditorAsync(originalAddress);
        if (address is null || address.Equals(originalAddress, StringComparison.Ordinal))
        {
            return;
        }

        TrackerEntries[index] = new TrackerEntry
        {
            Address = address,
            IsSelected = TrackerEntries[index].IsSelected
        };
        NotifyTrackerCollectionChanged();
    }

    private void DeleteBtTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        string address = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        int index = FindTrackerIndex(address);
        if (index < 0)
        {
            return;
        }

        TrackerEntries.RemoveAt(index);
        NotifyTrackerCollectionChanged();
    }

    private void BtTrackerEnabledToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        string address = toggleSwitch.Tag?.ToString() ?? string.Empty;
        int index = FindTrackerIndex(address);
        if (index < 0 || TrackerEntries[index].IsSelected == toggleSwitch.IsOn)
        {
            return;
        }

        TrackerEntries[index].IsSelected = toggleSwitch.IsOn;
        BitTorrentSettingChanged?.Invoke(this, new RoutedEventArgs());
    }

    private async Task<string?> ShowTrackerSourceEditorAsync(string? originalUrl)
    {
        TextBox urlTextBox = new()
        {
            Header = Strings.Get("TrackerSourceUrlFieldHeader"),
            PlaceholderText = "https://example.com/trackers.txt",
            Text = originalUrl ?? string.Empty,
            SelectionStart = originalUrl?.Length ?? 0
        };
        InfoBar validationInfoBar = CreateValidationInfoBar();
        StackPanel content = new()
        {
            Spacing = 12,
            Children =
            {
                urlTextBox,
                validationInfoBar
            }
        };
        ContentDialog dialog = CreateEditorDialog(
            originalUrl is null ? "TrackerAddSourceDialogTitle" : "TrackerEditSourceDialogTitle",
            originalUrl is null ? "AddButtonText" : "SaveButtonText",
            content);

        void ValidateInput()
        {
            string candidate = NormalizeValue(urlTextBox.Text);
            string message = GetTrackerSourceValidationMessage(candidate, originalUrl);
            SetValidationState(dialog, validationInfoBar, candidate, message);
        }

        urlTextBox.TextChanged += (_, _) => ValidateInput();
        dialog.Opened += (_, _) =>
        {
            _ = urlTextBox.Focus(FocusState.Programmatic);
            urlTextBox.SelectAll();
        };
        ValidateInput();

        return await dialog.ShowAsync() == ContentDialogResult.Primary
            ? NormalizeValue(urlTextBox.Text)
            : null;
    }

    private async Task<string?> ShowTrackerEditorAsync(string? originalAddress)
    {
        TextBox addressTextBox = new()
        {
            Header = Strings.Get("TrackerAddressFieldHeader"),
            PlaceholderText = "udp://tracker.example:6969/announce",
            Text = originalAddress ?? string.Empty,
            SelectionStart = originalAddress?.Length ?? 0
        };
        InfoBar validationInfoBar = CreateValidationInfoBar();
        StackPanel content = new()
        {
            Spacing = 12,
            Children =
            {
                addressTextBox,
                validationInfoBar
            }
        };
        ContentDialog dialog = CreateEditorDialog(
            originalAddress is null ? "TrackerAddDialogTitle" : "TrackerEditDialogTitle",
            originalAddress is null ? "AddButtonText" : "SaveButtonText",
            content);

        void ValidateInput()
        {
            string candidate = NormalizeValue(addressTextBox.Text);
            string message = GetTrackerValidationMessage(candidate, originalAddress);
            SetValidationState(dialog, validationInfoBar, candidate, message);
        }

        addressTextBox.TextChanged += (_, _) => ValidateInput();
        dialog.Opened += (_, _) =>
        {
            _ = addressTextBox.Focus(FocusState.Programmatic);
            addressTextBox.SelectAll();
        };
        ValidateInput();

        return await dialog.ShowAsync() == ContentDialogResult.Primary
            ? NormalizeValue(addressTextBox.Text)
            : null;
    }

    private ContentDialog CreateEditorDialog(string titleKey, string primaryButtonKey, object content)
    {
        return new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = Strings.Get(titleKey),
            Content = content,
            PrimaryButtonText = Strings.Get(primaryButtonKey),
            CloseButtonText = Strings.Get("CancelButtonText"),
            DefaultButton = ContentDialogButton.Primary
        };
    }

    private static InfoBar CreateValidationInfoBar() => new()
    {
        IsClosable = false,
        IsOpen = false,
        Severity = InfoBarSeverity.Error
    };

    private static void SetValidationState(
        ContentDialog dialog,
        InfoBar validationInfoBar,
        string candidate,
        string message)
    {
        dialog.IsPrimaryButtonEnabled = string.IsNullOrEmpty(message);
        validationInfoBar.Message = message;
        validationInfoBar.IsOpen = !string.IsNullOrEmpty(message) && !string.IsNullOrWhiteSpace(candidate);
    }

    private string GetTrackerSourceValidationMessage(string candidate, string? originalUrl)
    {
        if (!IsValidHttpUrl(candidate))
        {
            return Strings.Get("TrackerSourceUrlInvalidMessage");
        }

        bool isDuplicate = TrackerSourceEntries.Any(entry =>
            entry.Url.Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
            !entry.Url.Equals(originalUrl, StringComparison.OrdinalIgnoreCase));
        return isDuplicate ? Strings.Get("TrackerSourceUrlDuplicateMessage") : string.Empty;
    }

    private string GetTrackerValidationMessage(string candidate, string? originalAddress)
    {
        if (!IsValidTrackerAddress(candidate))
        {
            return Strings.Get("TrackerAddressInvalidMessage");
        }

        bool isDuplicate = TrackerEntries.Any(entry =>
            entry.Address.Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
            !entry.Address.Equals(originalAddress, StringComparison.OrdinalIgnoreCase));
        return isDuplicate ? Strings.Get("TrackerAddressDuplicateMessage") : string.Empty;
    }

    private static TrackerSourceEntry CreateTrackerSourceEntry(string url, bool isSelected)
    {
        string? builtInLabel = BuiltInTrackerSources
            .FirstOrDefault(source => source.Url.Equals(url, StringComparison.OrdinalIgnoreCase))
            .Label;
        string displayName = !string.IsNullOrWhiteSpace(builtInLabel)
            ? builtInLabel
            : new Uri(url).Host;

        return new TrackerSourceEntry
        {
            DisplayName = displayName,
            Url = url,
            IsSelected = isSelected,
            ModifyVisibility = IsBuiltInTrackerSource(url) ? Visibility.Collapsed : Visibility.Visible
        };
    }

    private int FindTrackerSourceIndex(string url)
    {
        for (int index = 0; index < TrackerSourceEntries.Count; index++)
        {
            if (TrackerSourceEntries[index].Url.Equals(url, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private int FindTrackerIndex(string address)
    {
        for (int index = 0; index < TrackerEntries.Count; index++)
        {
            if (TrackerEntries[index].Address.Equals(address, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsBuiltInTrackerSource(string url) =>
        BuiltInTrackerSources.Any(source => source.Url.Equals(url, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> NormalizeHttpUrls(IEnumerable<string>? urls) =>
        urls?
            .Select(NormalizeValue)
            .Where(IsValidHttpUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase) ?? [];

    private static string NormalizeValue(string? value) => value?.Trim() ?? string.Empty;

    private static bool IsValidHttpUrl(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));

    private static bool IsValidTrackerAddress(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme.Equals("udp", StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals("ws", StringComparison.OrdinalIgnoreCase) ||
         uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase));

    private void NotifyTrackerCollectionChanged()
    {
        UpdateTrackerEmptyStates();
        BitTorrentSettingChanged?.Invoke(this, new RoutedEventArgs());
    }

    private void UpdateTrackerEmptyStates()
    {
        BtTrackerListEmptyText.Visibility = TrackerEntries.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void SyncBtTrackerButton_Click(object sender, RoutedEventArgs args)
    {
        SyncBtTrackerRequested?.Invoke(sender, args);
    }

    private void BitTorrentSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        UpdateToggleStateText(sender as ToggleSwitch);
        BitTorrentSettingChanged?.Invoke(sender, args);
    }

    private void BitTorrentSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void BitTorrentSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        BitTorrentSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void UpdateToggleStateText(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch is null)
        {
            return;
        }

        TextBlock? stateText = GetToggleStateText(toggleSwitch);
        if (stateText is not null)
        {
            stateText.Text = toggleSwitch.IsOn ? Strings.Get("ToggleOnState.Text") : Strings.Get("ToggleOffState.Text");
        }
    }

    private TextBlock? GetToggleStateText(ToggleSwitch toggleSwitch)
    {
        if (ReferenceEquals(toggleSwitch, BtAutoDownloadToggleSwitch)) return BtAutoDownloadStateText;
        if (ReferenceEquals(toggleSwitch, BtForceEncryptionToggleSwitch)) return BtForceEncryptionStateText;
        if (ReferenceEquals(toggleSwitch, BtAutoSyncTrackerToggleSwitch)) return BtAutoSyncTrackerStateText;
        return null;
    }
}
