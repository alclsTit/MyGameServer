using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Log;
using ServerEngine.Network.SystemLib;

namespace ServerEngine.Network.Server
{
    /// <summary>
    /// 중계서버, 연결된 다수의 서버를 컨트롤
    /// </summary>
    public class ControlServer : ServerBase
    {
        public ControlServer(ILogger logger) : base(logger) { }

        public override bool Initialize(string name)
        {
            if (!base.Initialize(name))
                return false;

            mServerState = ServerState.Initialized;

            return true;
        }

        public override bool Setup<TServerModule, TServerInfo, TNetworkSystem>(List<IListenInfo> listenInfo, TServerInfo config, Func<ServerEngine.Network.ServerSession.Session> creater)
        {
            if (!base.Setup<TServerModule, TServerInfo, TNetworkSystem>(listenInfo, config, creater))
                return false;

            //Todo: Setup 추가작업 진행

            mServerState = ServerState.SetupFinished;

            return true;
        }

        public override bool Setup<TServerModule, TServerInfo, TNetworkSystem>(string ip, ushort port, string serverName, TServerInfo config, Func<ServerEngine.Network.ServerSession.Session> creater, int backlog = 200, bool nodelay = true)
        {
            if (!base.Setup<TServerModule, TServerInfo, TNetworkSystem>(ip, port, serverName, config, creater, backlog, nodelay))
                return false;

            //Todo: Setup 추가작업 진행

            mServerState = ServerState.SetupFinished;

            return true;
        }

        public override void Start()
        {
            base.Start();

            //Todo: Start 추가작업 진행
        }

        public override void Stop()
        {
            base.Stop();

            //Todo: Stop 추가작업 진행
        }
    }
}
