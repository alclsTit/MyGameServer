using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using ServerEngine.Network.SystemLib;

namespace ServerEngine.Common
{
    //DNS (Domain Network System) - 도메인 이름으로 ip를 찾는다
    //도메인은 IP를 여러개 가질 수 있다. IP를 직접적으로 사용하면 추후 네트워크 주소가 변경될 때마다 IP를 바꿔줘야한다.
    //하지만, 도메인처리를 하면 해당 도메인에 IP를 등록만하면 된다.
    //ex) www.myNetwork.com 111.222.333.444 555.666.777.888 ...
    public static class ServerHostFinder
    {
        public static IPEndPoint GetIPAddressInHostEntry(string hostName, ushort port, bool ipv4 = true)
        {
            var hostEntryList = Dns.GetHostEntry(hostName).AddressList;
            if (ipv4)
            {
                var hostEntry = hostEntryList.FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return new IPEndPoint(hostEntry, port);
            }
            else
            {
                var hostEntry = hostEntryList.FirstOrDefault(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);
                return new IPEndPoint(hostEntry, port);
            }
        }

        public static IPEndPoint GetServerIPAddressDirect(string serverRawIP, ushort port)
        {
            if ("any".Equals(serverRawIP, StringComparison.OrdinalIgnoreCase))
                return new IPEndPoint(IPAddress.Any, port);
            else if ("ipv6any".Equals(serverRawIP, StringComparison.OrdinalIgnoreCase))
                return new IPEndPoint(IPAddress.IPv6Any, port);
            else
                return new IPEndPoint(IPAddress.Parse(serverRawIP), port);
        }

        public static IPEndPoint GetServerIPAddressDirect(IListenInfo listenInfo)
        {
            if (listenInfo == null)
                throw new ArgumentNullException(nameof(listenInfo));

            var ip = listenInfo.ip;
            var port = listenInfo.port;

            if ("any".Equals(ip, StringComparison.OrdinalIgnoreCase))
                return new IPEndPoint(IPAddress.Any, port);
            else if ("ipv6any".Equals(ip, StringComparison.OrdinalIgnoreCase))
                return new IPEndPoint(IPAddress.IPv6Any, port);
            else
                return new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public static IPEndPoint GetServerIPAddress(ushort port, bool ipv4 = true)
        {
            return GetIPAddressInHostEntry(Dns.GetHostName(), port, ipv4);
        }
    }
}
