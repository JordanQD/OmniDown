using System;
using System.Net;
using System.Net.Sockets;

namespace OmniDown.Services.Engine;

internal static class PortAvailabilityService
{
    public static bool IsTcpAvailable(int port) => port == 0 || CanBind(() =>
    {
        TcpListener listener = new(IPAddress.Any, port) { ExclusiveAddressUse = true };
        listener.Start();
        return listener;
    }, listener => listener.Stop());

    public static bool IsUdpAvailable(int port) => port == 0 || CanBind(() =>
    {
        UdpClient client = new(AddressFamily.InterNetwork) { ExclusiveAddressUse = true };
        client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
        return client;
    }, client => client.Dispose());

    private static bool CanBind<T>(Func<T> bind, Action<T> release)
    {
        T? resource = default;
        try
        {
            resource = bind();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
        finally
        {
            if (resource is not null)
            {
                release(resource);
            }
        }
    }
}
