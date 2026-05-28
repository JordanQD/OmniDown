namespace OmniDown.Models.Settings;

using System.Security.Cryptography;

public enum Aria2EngineType
{
    Aria2c,
    Aria2Next,
    Custom
}

public sealed record AdvancedSettings(
    string Aria2Path,
    Aria2EngineType EngineType,
    int RpcPort,
    string RpcSecret,
    bool AutoSubmitFromExtension,
    int ExtensionApiPort,
    string ExtensionApiSecret,
    string LogLevel,
    bool ClipboardDetectionEnabled,
    bool ClipboardHttpEnabled,
    bool ClipboardFtpEnabled,
    bool ClipboardMagnetEnabled,
    bool ClipboardThunderEnabled,
    bool ClipboardBtHashEnabled,
    bool ProtocolMagnetEnabled,
    bool ProtocolThunderEnabled,
    bool ProtocolOmniDownEnabled)
{
    public static AdvancedSettings Default { get; } = new(
        Aria2Path: string.Empty,
        EngineType: Aria2EngineType.Aria2Next,
        RpcPort: 6800,
        RpcSecret: GenerateSecret(),
        AutoSubmitFromExtension: false,
        ExtensionApiPort: 16800,
        ExtensionApiSecret: GenerateSecret(),
        LogLevel: "notice",
        ClipboardDetectionEnabled: false,
        ClipboardHttpEnabled: true,
        ClipboardFtpEnabled: true,
        ClipboardMagnetEnabled: true,
        ClipboardThunderEnabled: false,
        ClipboardBtHashEnabled: false,
        ProtocolMagnetEnabled: false,
        ProtocolThunderEnabled: false,
        ProtocolOmniDownEnabled: true);

    public static string GenerateSecret()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        byte[] bytes = RandomNumberGenerator.GetBytes(16);
        char[] result = new char[bytes.Length];
        for (int index = 0; index < bytes.Length; index++)
        {
            result[index] = chars[bytes[index] % chars.Length];
        }

        return new string(result);
    }
}
