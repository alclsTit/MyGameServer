using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using ServerEngine.Network.SystemLib;
using ServerEngine.Network.ServerSession;

namespace ServerEngine.Network.Server
{
    /// <summary>
    /// ServerModule 인터페이스
    /// ServerModule은 Connect / Accept 가 있을 수 있다
    /// </summary>
    public interface IServerModule
    {
        string Name { get; }

        public Log.ILogger Logger { get; }

        IPEndPoint ipEndPoint { get; }

        //void Initialize(List<IListenInfo> listenInfoList, IListenInfo listenInfo, Config.ServerConfig serverInfo, INetworkSystemBase networkSystem,  Logger logger, Func<Session> creater);

        //void InitializeSessionManager(ISessionManager sessionManager);

        bool StartOnce();

        bool Start();

        void Stop();

        bool ChangeState(int oldState, int newState);
    }
}
