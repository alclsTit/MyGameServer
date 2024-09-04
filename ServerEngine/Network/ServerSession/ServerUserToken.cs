using ServerEngine.Common;
using ServerEngine.Config;
using ServerEngine.Network.SystemLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.ServerSession
{
    public class ServerUserTokenManager : UserTokenManager
    {
        public ConcurrentDictionary<eServerType, List<ServerUserToken>> UserTokens { get; private set; } = new ConcurrentDictionary<eServerType, List<ServerUserToken>>();

        public ServerUserTokenManager(Log.ILogger logger, IConfigNetwork config_network)
            : base(logger, config_network)
        {
        }

        public bool Initialize()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in ServerUserTokenManager.Initialize() - {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }
    }

    public class ServerUserToken : UserToken
    {
        private IPEndPoint? mLocalEndPoint;
        private IPEndPoint? mRemoteEndPoint;

        public ServerUserToken(SocketBase socket, bool client_connect = false)
            : base(socket)
        {
            IsClientConnect = client_connect;


        }
    }
}
