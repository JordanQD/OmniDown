using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Models;
using OmniDown.Services.Localization;
using OmniDown.Services.Rpc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace OmniDown.Controls;

public sealed partial class Ed2kSettingsSectionControl : UserControl
{
    // Optional resource-discovery feature. ED2K link downloads do not depend on it.
    private static readonly bool IsEd2kSearchFeatureEnabled = false;

    public ObservableCollection<Ed2kServerEntry> ServerEntries { get; } = [];
    public ObservableCollection<Ed2kSearchResultEntry> SearchResults { get; } = [];

    public Ed2kSettingsSectionControl()
    {
        InitializeComponent();
        Ed2kSearchSection.Visibility = IsEd2kSearchFeatureEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
        UpdateServerListEmptyState();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries => GetSearchEntries();

    private IEnumerable<SettingSearchEntry> GetSearchEntries()
    {
        yield return new(Ed2kListenPortSettingCard, "ed2k", "port", "端口", "监听");
        yield return new(Ed2kUdpListenPortSettingCard, "ed2k", "udp", "port", "端口");
        yield return new(Ed2kUploadSlotsSettingCard, "ed2k", "upload", "slots", "上传", "槽位");
        yield return new(Ed2kServerListUrlSettingCard, "ed2k", "server.met", "url", "服务器", "来源");
        yield return new(Ed2kKadBootstrapUrlSettingCard, "ed2k", "nodes.dat", "kad", "url", "节点", "来源");
        yield return new(Ed2kServerListSettingCard, "ed2k", "server", "服务器", "列表");
        yield return new(Ed2kAutoSyncSettingCard, "ed2k", "sync", "auto", "同步", "自动");

        if (!IsEd2kSearchFeatureEnabled)
        {
            yield break;
        }

        yield return new(Ed2kSearchKeywordSettingCard, "ed2k", "search", "keyword", "搜索", "关键词");
        yield return new(Ed2kFileTypeSettingCard, "ed2k", "search", "file", "type", "文件", "类型");
        yield return new(Ed2kMinSourcesSettingCard, "ed2k", "search", "source", "来源", "最少");
        yield return new(Ed2kSearchTimeoutSettingCard, "ed2k", "search", "timeout", "time", "时长", "搜索");
    }

    internal StackPanel Ed2kSettingsContentControl => Ed2kSettingsContent;
    internal NumberBox Ed2kListenPortNumberBoxControl => Ed2kListenPortNumberBox;
    internal NumberBox Ed2kUdpListenPortNumberBoxControl => Ed2kUdpListenPortNumberBox;
    internal NumberBox Ed2kUploadSlotsNumberBoxControl => Ed2kUploadSlotsNumberBox;
    internal TextBox Ed2kServerListUrlTextBoxControl => Ed2kServerListUrlTextBox;
    internal TextBox Ed2kKadBootstrapUrlTextBoxControl => Ed2kKadBootstrapUrlTextBox;
    internal IReadOnlyList<string> Ed2kServerAddresses => ServerEntries.Select(entry => entry.Address).ToArray();
    internal IReadOnlyList<string> DisabledEd2kServerAddresses => ServerEntries
        .Where(entry => !entry.IsSelected)
        .Select(entry => entry.Address)
        .ToArray();
    internal ToggleSwitch Ed2kAutoSyncToggleSwitchControl => Ed2kAutoSyncToggleSwitch;
    internal TextBlock Ed2kAutoSyncStateTextControl => Ed2kAutoSyncStateText;
    internal ComboBox Ed2kSyncIntervalComboBoxControl => Ed2kSyncIntervalComboBox;
    internal Button Ed2kSyncNowButtonControl => Ed2kSyncNowButton;
    internal TextBlock Ed2kLastSyncTextControl => Ed2kLastSyncText;
    internal TextBox Ed2kSearchKeywordTextBoxControl => Ed2kSearchKeywordTextBox;
    internal Button Ed2kSearchKeywordButtonControl => Ed2kSearchKeywordButton;
    internal ComboBox Ed2kFileTypeComboBoxControl => Ed2kFileTypeComboBox;
    internal NumberBox Ed2kMinSourcesNumberBoxControl => Ed2kMinSourcesNumberBox;
    internal NumberBox Ed2kSearchTimeoutNumberBoxControl => Ed2kSearchTimeoutNumberBox;
    internal bool IsEd2kSearchActive { get; private set; }

    internal event RoutedEventHandler? Ed2kSettingChanged;
    internal event RoutedEventHandler? RandomEd2kPortRequested;
    internal event RoutedEventHandler? RandomEd2kUdpPortRequested;
    internal event RoutedEventHandler? SyncEd2kRequested;
    internal event RoutedEventHandler? SearchEd2kRequested;
    internal event EventHandler<Ed2kSearchDownloadRequestedEventArgs>? DownloadEd2kSearchResultRequested;

    internal void SetEd2kServerAddresses(IEnumerable<string> addresses, IEnumerable<string>? disabledAddresses = null)
    {
        HashSet<string> disabled = disabledAddresses?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
        ServerEntries.Clear();
        foreach (string address in addresses
            .Select(NormalizeServerAddress)
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            ServerEntries.Add(new Ed2kServerEntry(address, !disabled.Contains(address)));
        }

        UpdateServerListEmptyState();
    }

    private void Ed2kSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        UpdateToggleStateText(sender as ToggleSwitch);
        Ed2kSettingChanged?.Invoke(sender, args);
    }

    private void Ed2kSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        Ed2kSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void Ed2kSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        Ed2kSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void Ed2kSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        Ed2kSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void RandomEd2kPortButton_Click(object sender, RoutedEventArgs args)
    {
        RandomEd2kPortRequested?.Invoke(sender, args);
    }

    private void RandomEd2kUdpPortButton_Click(object sender, RoutedEventArgs args)
    {
        RandomEd2kUdpPortRequested?.Invoke(sender, args);
    }

    private void SyncEd2kButton_Click(object sender, RoutedEventArgs args)
    {
        SyncEd2kRequested?.Invoke(sender, args);
    }

    private void SearchEd2kButton_Click(object sender, RoutedEventArgs args)
    {
        SearchEd2kRequested?.Invoke(sender, args);
    }

    private void DownloadEd2kSearchResultButton_Click(object sender, RoutedEventArgs args)
    {
        string link = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        Ed2kSearchResultEntry? result = SearchResults.FirstOrDefault(item =>
            item.Ed2kLink.Equals(link, StringComparison.OrdinalIgnoreCase));
        if (result is not null)
        {
            DownloadEd2kSearchResultRequested?.Invoke(
                this,
                new Ed2kSearchDownloadRequestedEventArgs(result));
        }
    }

    internal void SetEd2kSearchState(bool isActive, TimeSpan elapsed, TimeSpan duration, string status)
    {
        IsEd2kSearchActive = isActive;
        Ed2kSearchKeywordButton.Content = Strings.Get(isActive ? "Ed2kSearchCancelButtonText" : "Ed2kSearchStartButtonText");
        AutomationProperties.SetName(
            Ed2kSearchKeywordButton,
            Strings.Get(isActive ? "Ed2kSearchCancelButtonText" : "Ed2kSearchStartButtonText"));
        Ed2kSearchStatusCard.Visibility = string.IsNullOrWhiteSpace(status)
            ? Visibility.Collapsed
            : Visibility.Visible;
        Ed2kSearchStatusText.Text = status;
        Ed2kSearchProgressBar.Value = duration.TotalMilliseconds <= 0
            ? 0
            : Math.Clamp(elapsed.TotalMilliseconds / duration.TotalMilliseconds * 100, 0, 100);
    }

    internal void SetEd2kSearchResults(IEnumerable<Aria2Ed2kSearchResult> results)
    {
        SearchResults.Clear();
        foreach (Aria2Ed2kSearchResult result in results)
        {
            SearchResults.Add(new Ed2kSearchResultEntry(result));
        }

        Ed2kSearchResultsSettingCard.Visibility = SearchResults.Count == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
        Ed2kSearchResultsSettingCard.Header = Strings.Format("Ed2kSearchResultsHeaderText", SearchResults.Count);
    }

    private async void AddEd2kServerButton_Click(object sender, RoutedEventArgs args)
    {
        string? address = await ShowServerEditorAsync(null);
        if (address is null)
        {
            return;
        }

        ServerEntries.Add(new Ed2kServerEntry(address));
        UpdateServerListEmptyState();
        Ed2kSettingChanged?.Invoke(this, new RoutedEventArgs());
    }

    private async void EditEd2kServerButton_Click(object sender, RoutedEventArgs args)
    {
        string originalAddress = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        int index = FindServerIndex(originalAddress);
        if (index < 0)
        {
            return;
        }

        string? address = await ShowServerEditorAsync(originalAddress);
        if (address is null || address.Equals(originalAddress, StringComparison.Ordinal))
        {
            return;
        }

        ServerEntries[index] = new Ed2kServerEntry(address, ServerEntries[index].IsSelected);
        Ed2kSettingChanged?.Invoke(this, new RoutedEventArgs());
    }

    private void DeleteEd2kServerButton_Click(object sender, RoutedEventArgs args)
    {
        string address = (sender as FrameworkElement)?.Tag?.ToString() ?? string.Empty;
        int index = FindServerIndex(address);
        if (index < 0)
        {
            return;
        }

        ServerEntries.RemoveAt(index);
        UpdateServerListEmptyState();
        Ed2kSettingChanged?.Invoke(this, new RoutedEventArgs());
    }

    private void Ed2kServerEnabledToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        if (sender is not ToggleSwitch toggleSwitch)
        {
            return;
        }

        string address = toggleSwitch.Tag?.ToString() ?? string.Empty;
        int index = FindServerIndex(address);
        if (index < 0 || ServerEntries[index].IsSelected == toggleSwitch.IsOn)
        {
            return;
        }

        ServerEntries[index].IsSelected = toggleSwitch.IsOn;
        Ed2kSettingChanged?.Invoke(this, new RoutedEventArgs());
    }

    private async Task<string?> ShowServerEditorAsync(string? originalAddress)
    {
        TextBox addressTextBox = new()
        {
            Header = Strings.Get("Ed2kServerAddressFieldHeader"),
            PlaceholderText = Strings.Get("Ed2kServerAddressPlaceholder"),
            Text = originalAddress ?? string.Empty,
            SelectionStart = originalAddress?.Length ?? 0
        };
        InfoBar validationInfoBar = new()
        {
            IsClosable = false,
            IsOpen = false,
            Severity = InfoBarSeverity.Error
        };
        StackPanel content = new()
        {
            Spacing = 12,
            Children =
            {
                addressTextBox,
                validationInfoBar
            }
        };
        ContentDialog dialog = new()
        {
            XamlRoot = XamlRoot,
            Title = Strings.Get(originalAddress is null ? "Ed2kAddServerDialogTitle" : "Ed2kEditServerDialogTitle"),
            Content = content,
            PrimaryButtonText = Strings.Get(originalAddress is null ? "AddButtonText" : "SaveButtonText"),
            CloseButtonText = Strings.Get("CancelButtonText"),
            DefaultButton = ContentDialogButton.Primary
        };

        void ValidateInput()
        {
            string candidate = NormalizeServerAddress(addressTextBox.Text);
            string message = GetServerValidationMessage(candidate, originalAddress);
            dialog.IsPrimaryButtonEnabled = string.IsNullOrEmpty(message);
            validationInfoBar.Message = message;
            validationInfoBar.IsOpen = !string.IsNullOrEmpty(message) && !string.IsNullOrWhiteSpace(candidate);
        }

        addressTextBox.TextChanged += (_, _) => ValidateInput();
        dialog.Opened += (_, _) =>
        {
            _ = addressTextBox.Focus(FocusState.Programmatic);
            addressTextBox.SelectAll();
        };
        ValidateInput();

        return await dialog.ShowAsync() == ContentDialogResult.Primary
            ? NormalizeServerAddress(addressTextBox.Text)
            : null;
    }

    private string GetServerValidationMessage(string candidate, string? originalAddress)
    {
        if (!IsValidServerAddress(candidate))
        {
            return Strings.Get("Ed2kServerAddressInvalidMessage");
        }

        bool isDuplicate = ServerEntries.Any(entry =>
            entry.Address.Equals(candidate, StringComparison.OrdinalIgnoreCase) &&
            !entry.Address.Equals(originalAddress, StringComparison.OrdinalIgnoreCase));
        return isDuplicate ? Strings.Get("Ed2kServerAddressDuplicateMessage") : string.Empty;
    }

    private int FindServerIndex(string address)
    {
        for (int index = 0; index < ServerEntries.Count; index++)
        {
            if (ServerEntries[index].Address.Equals(address, StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return -1;
    }

    private static string NormalizeServerAddress(string? value) => value?.Trim() ?? string.Empty;

    private static bool IsValidServerAddress(string value)
    {
        int separator = value.LastIndexOf(':');
        return separator > 0 &&
            separator < value.Length - 1 &&
            int.TryParse(value[(separator + 1)..], out int port) &&
            port is > 0 and <= 65535;
    }

    private void UpdateServerListEmptyState()
    {
        Ed2kServerListEmptyText.Visibility = ServerEntries.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void UpdateToggleStateText(ToggleSwitch? toggleSwitch)
    {
        if (toggleSwitch is null) return;

        TextBlock? stateText = GetToggleStateText(toggleSwitch);
        if (stateText is not null)
        {
            stateText.Text = toggleSwitch.IsOn ? Strings.Get("ToggleOnState.Text") : Strings.Get("ToggleOffState.Text");
        }
    }

    private TextBlock? GetToggleStateText(ToggleSwitch toggleSwitch)
    {
        if (ReferenceEquals(toggleSwitch, Ed2kAutoSyncToggleSwitch)) return Ed2kAutoSyncStateText;
        return null;
    }
}

public sealed class Ed2kSearchDownloadRequestedEventArgs(Ed2kSearchResultEntry result) : EventArgs
{
    public Ed2kSearchResultEntry Result { get; } = result;
}
