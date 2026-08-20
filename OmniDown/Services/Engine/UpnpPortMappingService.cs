using OmniDown.Services.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace OmniDown.Services.Engine;

internal sealed class UpnpPortMappingService : IDisposable
{
    private int _tcpPort;
    private int _udpPort;

    public Task ConfigureEd2kAsync(int tcpPort, int udpPort) =>
        Task.Run(() => ConfigureEd2k(tcpPort, udpPort));

    public void Dispose() => RemoveMappings();

    private void ConfigureEd2k(int tcpPort, int udpPort)
    {
        RemoveMappings();
        string localAddress = ResolveLocalIpv4Address();
        if (string.IsNullOrWhiteSpace(localAddress))
        {
            AppLogger.Warning("UPnP", "ED2K mapping skipped because no LAN IPv4 address was found.");
            return;
        }

        object? nat = null;
        object? mappings = null;
        try
        {
            (nat, mappings) = OpenMappingCollection();
            if (mappings is null)
            {
                AppLogger.Warning("UPnP", "The router did not expose a static port mapping collection.");
                return;
            }

            if (tcpPort > 0)
            {
                AddMapping(mappings, tcpPort, "TCP", localAddress, "OmniDown ED2K TCP");
                _tcpPort = tcpPort;
            }

            if (udpPort > 0)
            {
                AddMapping(mappings, udpPort, "UDP", localAddress, "OmniDown ED2K UDP");
                _udpPort = udpPort;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Warning("UPnP", $"ED2K mapping was unavailable: {ex.Message}");
        }
        finally
        {
            ReleaseComObject(mappings);
            ReleaseComObject(nat);
        }
    }

    private void RemoveMappings()
    {
        if (_tcpPort == 0 && _udpPort == 0) return;

        object? nat = null;
        object? mappings = null;
        try
        {
            (nat, mappings) = OpenMappingCollection();
            if (mappings is not null)
            {
                if (_tcpPort > 0) RemoveMapping(mappings, _tcpPort, "TCP");
                if (_udpPort > 0) RemoveMapping(mappings, _udpPort, "UDP");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Debug("UPnP", $"ED2K mapping cleanup skipped: {ex.Message}");
        }
        finally
        {
            _tcpPort = 0;
            _udpPort = 0;
            ReleaseComObject(mappings);
            ReleaseComObject(nat);
        }
    }

    private static (object? Nat, object? Mappings) OpenMappingCollection()
    {
        Type? natType = Type.GetTypeFromProgID("HNetCfg.NATUPnP");
        object? nat = natType is null ? null : Activator.CreateInstance(natType);
        object? mappings = nat?.GetType().InvokeMember(
            "StaticPortMappingCollection",
            System.Reflection.BindingFlags.GetProperty,
            null,
            nat,
            null);
        return (nat, mappings);
    }

    private static void AddMapping(object mappings, int port, string protocol, string address, string description)
    {
        mappings.GetType().InvokeMember(
            "Add",
            System.Reflection.BindingFlags.InvokeMethod,
            null,
            mappings,
            [port, protocol, port, address, true, description]);
        AppLogger.Info("UPnP", $"mapped ED2K {protocol} port {port} to {address}");
    }

    private static void RemoveMapping(object mappings, int port, string protocol)
    {
        mappings.GetType().InvokeMember(
            "Remove",
            System.Reflection.BindingFlags.InvokeMethod,
            null,
            mappings,
            [port, protocol]);
    }

    private static string ResolveLocalIpv4Address() =>
        Dns.GetHostEntry(Dns.GetHostName()).AddressList
            .FirstOrDefault(address =>
                address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
            ?.ToString() ?? string.Empty;

    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            _ = Marshal.FinalReleaseComObject(value);
        }
    }
}
