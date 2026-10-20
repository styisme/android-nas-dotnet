using System.Net;
using System.Net.Sockets;

namespace AndroidNas.Server.Protocols;

public static class NetUtils
{
    public static string GetLocalIp()
    {
        try
        {
            using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect("8.8.8.8", 65530);
            if (s.LocalEndPoint is IPEndPoint ep)
                return ep.Address.ToString();
        }
        catch { }

        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { }

        return "127.0.0.1";
    }
}
