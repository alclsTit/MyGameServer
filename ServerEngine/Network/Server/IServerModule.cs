using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using ServerEngine.Network.SystemLib;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;

namespace ServerEngine.Network.Server
{
    /// <summary>
    /// ServerModule 인터페이스
    /// ServerModule은 Connect / Accept 가 있을 수 있다
    /// </summary>
    public interface IServerModule
    {
        string Name { get; }

        public IPEndPoint? LocalEndPoint { get; }

        public NetworkSystemBase Acceptor { get; }

        Log.ILogger Logger { get; }

        List<IConfigListen> config_listen_list { get; }

        int ServiceStartTime { get; }

        bool Start();

        void Stop();
    }
}
