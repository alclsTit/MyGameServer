using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Network.SystemLib;

namespace ServerEngine.Config
{
    public class ListenConfig : IListenInfo
    {
        public string ip { get; private set; }

        public ushort port { get; private set; }

        public int backlog { get; private set; }

        public bool nodelay { get; private set; }

        public string serverName { get; private set; }

        public ListenConfig(string ip, ushort port, string serverName, int backlog = ServerDefaultOption.backlog, bool nodelay = true)
        {
            this.ip = ip;
            this.port = port;
            this.backlog = backlog;
            this.nodelay = nodelay;
            this.serverName = serverName;
        }
    }
}
