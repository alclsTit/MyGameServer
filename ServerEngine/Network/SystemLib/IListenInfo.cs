using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.SystemLib
{
    public interface IListenInfo
    {
        string ip { get; }

        ushort port { get; }

        int backlog { get; }

        bool nodelay { get; }

        string serverName { get; }        

    }
}
