using ServerEngine.Common;
using ServerEngine.Config;
using ServerEngine.Network.Message;
using ServerEngine.Network.SystemLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        private bool mDisposed = false;
        public ServerUserToken() : base() { }

        public bool Initialize(Log.ILogger logger, IConfigNetwork config_network, SocketBase socket,
                               SocketAsyncEventArgs send_event_args, SocketAsyncEventArgs recv_event_args,
                               SendStreamPool send_stream_pool, RecvStream recv_stream, Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, SendStreamPool?, bool> retrieve_event)
        {
            if (null == logger)
                throw new ArgumentNullException(nameof(logger));

            if (null == config_network)
                throw new ArgumentNullException(nameof(config_network));

            if (null == socket)
                throw new ArgumentNullException(nameof(socket));

            if (null == send_event_args)
                throw new ArgumentNullException(nameof(send_event_args));

            if (null == recv_event_args)
                throw new ArgumentNullException(nameof(recv_event_args));

            if (null == send_stream_pool) 
                throw new ArgumentNullException(nameof(send_stream_pool));

            if (null == recv_stream)
                throw new ArgumentNullException(nameof(recv_stream));

            if (null == retrieve_event)
                throw new ArgumentNullException(nameof(retrieve_event));

            if (false == base.InitializeBase(logger, config_network, socket, 
                                             send_event_args, recv_event_args, 
                                             send_stream_pool, recv_stream, retrieve_event))
            {
                Logger.Error($"Error in ServerUserToken.Initialize() - Fail to Initialize");
                return false;
            }

            base.TokenType = eTokenType.Server;

            return true;
        }

        // ClientUserToken과 ServerUserToken이 내부 로직에 차이가 있을 수 있어 일단 오버로딩
        public override void HeartbeatCheck(object? state)
        {
            try
            {
                base.HeartbeatCheck(state);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in ServerUserToken.HeartbeatCheck() - UserToken >> [{TokenType}:{mTokenId}]. {ex.Message} - {ex.StackTrace}");
                return; 
            }
        }

        public override void Dispose()
        {
            if (mDisposed)
                return;

            base.Dispose();

            mDisposed = true;
        }

    }
}
