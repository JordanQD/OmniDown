using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OmniDown.Services.Localization;
using System;
using System.Collections.Generic;

namespace OmniDown.Controls;

public sealed partial class AdvancedSettingsSectionControl : UserControl
{
    public AdvancedSettingsSectionControl()
    {
        InitializeComponent();
    }

    internal IEnumerable<SettingSearchEntry> SearchEntries =>
    [
        new(AriaPathSettingCard, "aria2c", "path", Strings.Get("AriaPathLabel.Text"), Strings.Get("AriaPathTextBox.PlaceholderText"), "路径"),
        new(RpcPortSettingCard, "rpc", "port", Strings.Get("RpcPortLabel.Text"), "端口"),
        new(RpcSecretSettingCard, "rpc", "secret", "token", "密钥", "令牌"),
        new(ProcessStatusSettingCard, "process", "status", "aria2", Strings.Get("ProcessStatusLabel.Text"), "状态"),
        new(ExtensionAutoSubmitSettingCard, "extension", "browser", "auto submit", "扩展", "浏览器", "自动提交"),
        new(ExtensionApiPortSettingCard, "extension", "api", "port", "browser", "扩展", "端口"),
        new(ExtensionApiSecretSettingCard, "extension", "api", "secret", "browser", "扩展", "密钥"),
        new(LogLevelSettingCard, "log", "level", "debug", "日志", "级别"),
        new(AdvancedPathsSettingCard, "config", "session", "folder", "path", "配置", "会话", "目录"),
        new(LogPathsSettingCard, "log", "file", "folder", "diagnostic", "日志", "文件", "目录"),
        new(SessionResetSettingCard, "session", "reset", "clear", "aria2", "会话", "清空"),
        new(ClipboardDetectionSettingCard, "clipboard", "detect", "paste", "剪贴板", "检测", "粘贴"),
        new(ClipboardTypesSettingCard, "clipboard", "http", "ftp", "magnet", "thunder", "hash", "剪贴板", "磁力", "迅雷"),
        new(ProtocolMagnetSettingCard, "default", "program", "protocol", "magnet", "默认程序", "协议", "磁力"),
        new(ProtocolThunderSettingCard, "default", "program", "protocol", "thunder", "默认程序", "协议", "迅雷"),
        new(ProtocolOmniDownSettingCard, "default", "program", "protocol", "omnidown", "extension", "默认程序", "协议", "扩展")
    ];

    internal StackPanel AdvancedSettingsContentControl => AdvancedSettingsContent;
    internal TextBox AriaPathTextBoxControl => AriaPathTextBox;
    internal NumberBox RpcPortNumberBoxControl => RpcPortNumberBox;
    internal PasswordBox RpcSecretPasswordBoxControl => RpcSecretPasswordBox;
    internal ToggleSwitch ExtensionAutoSubmitToggleSwitchControl => ExtensionAutoSubmitToggleSwitch;
    internal TextBlock ExtensionAutoSubmitStateTextControl => ExtensionAutoSubmitStateText;
    internal NumberBox ExtensionApiPortNumberBoxControl => ExtensionApiPortNumberBox;
    internal PasswordBox ExtensionApiSecretPasswordBoxControl => ExtensionApiSecretPasswordBox;
    internal ComboBox LogLevelComboBoxControl => LogLevelComboBox;
    internal TextBlock AdvancedPathsSummaryTextControl => AdvancedPathsSummaryText;
    internal TextBlock LogPathsSummaryTextControl => LogPathsSummaryText;
    internal ToggleSwitch ClipboardDetectionToggleSwitchControl => ClipboardDetectionToggleSwitch;
    internal TextBlock ClipboardDetectionStateTextControl => ClipboardDetectionStateText;
    internal ToggleSwitch ClipboardHttpToggleSwitchControl => ClipboardHttpToggleSwitch;
    internal ToggleSwitch ClipboardFtpToggleSwitchControl => ClipboardFtpToggleSwitch;
    internal ToggleSwitch ClipboardMagnetToggleSwitchControl => ClipboardMagnetToggleSwitch;
    internal ToggleSwitch ClipboardThunderToggleSwitchControl => ClipboardThunderToggleSwitch;
    internal ToggleSwitch ClipboardBtHashToggleSwitchControl => ClipboardBtHashToggleSwitch;
    internal ToggleSwitch ProtocolMagnetToggleSwitchControl => ProtocolMagnetToggleSwitch;
    internal TextBlock ProtocolMagnetStateTextControl => ProtocolMagnetStateText;
    internal ToggleSwitch ProtocolThunderToggleSwitchControl => ProtocolThunderToggleSwitch;
    internal TextBlock ProtocolThunderStateTextControl => ProtocolThunderStateText;
    internal ToggleSwitch ProtocolOmniDownToggleSwitchControl => ProtocolOmniDownToggleSwitch;
    internal TextBlock ProtocolOmniDownStateTextControl => ProtocolOmniDownStateText;
    internal TextBlock SettingsAriaStatusTextControl => SettingsAriaStatusText;
    internal StackPanel ProcessStatusSettingControlControl => ProcessStatusSettingControl;
    internal FontIcon AriaStartStopIconControl => AriaStartStopIcon;
    internal Button AriaStartStopButtonControl => AriaStartStopButton;
    internal Button AriaRestartButtonControl => AriaRestartButton;

    internal event RoutedEventHandler? AdvancedSettingChanged;
    internal event RoutedEventHandler? BrowseAriaPathRequested;
    internal event RoutedEventHandler? CopyRpcSecretRequested;
    internal event RoutedEventHandler? GenerateRpcSecretRequested;
    internal event RoutedEventHandler? CopyExtensionApiSecretRequested;
    internal event RoutedEventHandler? GenerateExtensionApiSecretRequested;
    internal event RoutedEventHandler? OpenConfigFolderRequested;
    internal event RoutedEventHandler? OpenLogFolderRequested;
    internal event RoutedEventHandler? ClearSessionRequested;
    internal event RoutedEventHandler? StartStopAriaRequested;
    internal event RoutedEventHandler? RestartAriaRequested;

    private void BrowseAriaPathButton_Click(object sender, RoutedEventArgs args)
    {
        BrowseAriaPathRequested?.Invoke(sender, args);
    }

    private void CopyRpcSecretButton_Click(object sender, RoutedEventArgs args)
    {
        CopyRpcSecretRequested?.Invoke(sender, args);
    }

    private void GenerateRpcSecretButton_Click(object sender, RoutedEventArgs args)
    {
        GenerateRpcSecretRequested?.Invoke(sender, args);
    }

    private void CopyExtensionApiSecretButton_Click(object sender, RoutedEventArgs args)
    {
        CopyExtensionApiSecretRequested?.Invoke(sender, args);
    }

    private void GenerateExtensionApiSecretButton_Click(object sender, RoutedEventArgs args)
    {
        GenerateExtensionApiSecretRequested?.Invoke(sender, args);
    }

    private void OpenConfigFolderButton_Click(object sender, RoutedEventArgs args)
    {
        OpenConfigFolderRequested?.Invoke(sender, args);
    }

    private void OpenLogFolderButton_Click(object sender, RoutedEventArgs args)
    {
        OpenLogFolderRequested?.Invoke(sender, args);
    }

    private void ClearSessionButton_Click(object sender, RoutedEventArgs args)
    {
        ClearSessionRequested?.Invoke(sender, args);
    }

    private void StartStopAriaButton_Click(object sender, RoutedEventArgs args)
    {
        StartStopAriaRequested?.Invoke(sender, args);
    }

    private void RestartAriaButton_Click(object sender, RoutedEventArgs args)
    {
        RestartAriaRequested?.Invoke(sender, args);
    }

    private void AdvancedSettingTextBox_TextChanged(object sender, TextChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingPasswordBox_PasswordChanged(object sender, RoutedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, args);
    }

    private void AdvancedSettingNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        AdvancedSettingChanged?.Invoke(sender, new RoutedEventArgs());
    }

    private void AdvancedSettingToggleSwitch_Toggled(object sender, RoutedEventArgs args)
    {
        UpdateToggleStateText(sender as ToggleSwitch);
        AdvancedSettingChanged?.Invoke(sender, args);
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
        if (ReferenceEquals(toggleSwitch, ExtensionAutoSubmitToggleSwitch)) return ExtensionAutoSubmitStateText;
        if (ReferenceEquals(toggleSwitch, ClipboardDetectionToggleSwitch)) return ClipboardDetectionStateText;
        if (ReferenceEquals(toggleSwitch, ProtocolMagnetToggleSwitch)) return ProtocolMagnetStateText;
        if (ReferenceEquals(toggleSwitch, ProtocolThunderToggleSwitch)) return ProtocolThunderStateText;
        if (ReferenceEquals(toggleSwitch, ProtocolOmniDownToggleSwitch)) return ProtocolOmniDownStateText;
        return null;
    }
}
