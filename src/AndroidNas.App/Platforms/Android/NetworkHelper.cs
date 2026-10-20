using System.Net;
using System.Net.Sockets;

namespace AndroidNas.App.Platforms.Android;

public static class NetworkHelper
{
    public static string GetLocalIp()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            if (socket.LocalEndPoint is IPEndPoint ep) return ep.Address.ToString();
        }
        catch { }
        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
        }
        catch { }
        return "127.0.0.1";
    }
}
