using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniDown.Services.Engine;

public sealed record SystemProxySettings(string? AllProxy, string? NoProxy)
{
    public bool HasProxy => !string.IsNullOrWhiteSpace(AllProxy);
}

public static class SystemProxyResolver
{
    public static SystemProxySettings Resolve()
    {
        using RegistryKey? internetSettings = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Internet Settings");

        if (internetSettings is null)
        {
            return new SystemProxySettings(null, null);
        }

        int proxyEnabled = Convert.ToInt32(internetSettings.GetValue("ProxyEnable", 0));
        if (proxyEnabled == 0)
        {
            return new SystemProxySettings(null, null);
        }

        string proxyServer = internetSettings.GetValue("ProxyServer", string.Empty)?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(proxyServer))
        {
            return new SystemProxySettings(null, null);
        }

        string? proxy = ResolveProxyServer(proxyServer);
        string? noProxy = ResolveProxyOverride(internetSettings.GetValue("ProxyOverride", string.Empty)?.ToString());
        return new SystemProxySettings(proxy, noProxy);
    }

    private static string? ResolveProxyServer(string proxyServer)
    {
        if (!proxyServer.Contains(';'))
        {
            return NormalizeProxyUri(proxyServer);
        }

        Dictionary<string, string> entries = proxyServer
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2 && !string.IsNullOrWhiteSpace(parts[1]))
            .ToDictionary(parts => parts[0].ToLowerInvariant(), parts => parts[1]);

        if (entries.TryGetValue("https", out string? httpsProxy))
        {
            return NormalizeProxyUri(httpsProxy);
        }

        if (entries.TryGetValue("http", out string? httpProxy))
        {
            return NormalizeProxyUri(httpProxy);
        }

        return null;
    }

    private static string? NormalizeProxyUri(string proxy, string defaultScheme = "http")
    {
        proxy = proxy.Trim();
        if (string.IsNullOrWhiteSpace(proxy))
        {
            return null;
        }

        if (proxy.Contains("://", StringComparison.Ordinal))
        {
            return proxy;
        }

        return $"{defaultScheme}://{proxy}";
    }

    private static string? ResolveProxyOverride(string? proxyOverride)
    {
        if (string.IsNullOrWhiteSpace(proxyOverride))
        {
            return null;
        }

        string[] entries = proxyOverride
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(entry => !entry.Equals("<local>", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return entries.Length == 0 ? null : string.Join(",", entries);
    }
}
