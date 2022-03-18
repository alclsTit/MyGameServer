using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ServerEngine.Network.SystemLib
{
    public class ListenInfo : IListenInfo
    {
        public string ip { get; private set; }

        public ushort port { get; private set; }

        public int backlog { get; private set; }

        public bool nodelay { get; private set; }

        public string serverName { get; private set; }

        public IPEndPoint ipEndpoint { get; private set; }

        public ListenInfo(string ip, ushort port, int backlog, string serverName, bool nodelay = true)
        {
            this.ip = ip;
            this.port = port;
            this.backlog = backlog;
            this.nodelay = nodelay;
            this.serverName = serverName;

            ipEndpoint = ServerEngine.Common.ServerHostFinder.GetServerIPAddress(port);
        }

        public ListenInfo(string ip, ushort port, int backlog, string serverName, IPEndPoint endpoint, bool nodelay = true)
        {
            this.ip = ip;
            this.port = port;
            this.backlog = backlog;
            this.nodelay = nodelay;
            this.serverName = serverName;

            this.ipEndpoint = endpoint;
        }
    }
}
