using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace ServerEngine.Network.SystemLib
{
    public interface INetworkSystemBase
    {
        void Initialize(ServerEngine.Network.Server.ServerModuleBase module, IListenInfo listenInfo, ServerEngine.Config.ServerConfig serverInfo, ServerEngine.Log.Logger logger, Func<ServerSession.Session> creater);

        bool StartOnce();

        bool Start();

        void Stop();

        event EventHandler StopCallback;
    }
}
