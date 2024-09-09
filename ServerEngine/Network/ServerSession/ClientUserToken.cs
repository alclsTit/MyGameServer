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
    public class ClientUserTokenManager : UserTokenManager
    {
        #region Lazy Singleton
        //public static readonly Lazy<ClientUserTokenManager> mInstance = new Lazy<ClientUserTokenManager>(() => new ClientUserTokenManager());
        //public static ClientUserTokenManager Instance => mInstance.Value;
        //private ClientUserTokenManager() { }
        #endregion

        private ConcurrentDictionary<int, List<ClientUserToken>> mThreadUserTokens = new ConcurrentDictionary<int, List<ClientUserToken>>();
        public ConcurrentDictionary<long, ClientUserToken> UserTokens { get; private set; } = new ConcurrentDictionary<long, ClientUserToken>();

        public ClientUserTokenManager(Log.ILogger logger, IConfigNetwork config_network)
            : base(logger, config_network)
        {
        }

        public bool Initialize(int max_connection)
        {
            if (0 >= max_connection)
                throw new ArgumentNullException(nameof(max_connection));

            try
            {
                int max_io_thread_count = base.m_config_network.max_io_thread_count;
                int user_per_thread = (int)(max_connection / max_io_thread_count);
                for (var i = 0; i < max_io_thread_count; ++i)
                    mThreadUserTokens.TryAdd(i, new List<ClientUserToken>(user_per_thread));

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in ClientUserTokenManager.Initialize() - {ex.Message} - {ex.StackTrace}", ex);
                return false;
            }
        }

        public bool TryAddUserToken(long uid, ClientUserToken token)
        {
            if (0 >= uid)
                throw new ArgumentException(nameof(uid));

            if (null == token)
                throw new ArgumentNullException(nameof(token));

            var index = (int)(uid % base.m_config_network.max_io_thread_count);
            mThreadUserTokens[index].Add(token);

            return UserTokens.TryAdd(uid, token);
        }

        public bool TryRemoveToken(long uid, Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, bool> retrieve_event)
        {
            if (UserTokens.TryGetValue(uid, out var token))
            {
                token.Dispose(retrieve_event);
            }
        }

        public async ValueTask Run(int index)
        {
            if (0 > index || base.m_config_network.max_io_thread_count <= index)
                throw new ArgumentException($"Index {index}");

            while (true)
            {
                foreach (var token in mThreadUserTokens[index])
                {
                    await token.ProcessSendAsync();
                }

                Thread.Sleep(10);
            }
        }
    }

    public class ClientUserToken : UserToken
    {
        public ClientUserToken() : base() { }

        public bool Initialize(Log.ILogger logger, IConfigNetwork config_network, SocketBase socket, SocketAsyncEventArgs send_event_args, SocketAsyncEventArgs recv_event_args, RecvStream recv_stream, long token_id)
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

            if (null == recv_stream)
                throw new ArgumentNullException(nameof(recv_stream));

            if (0 >= token_id)
                throw new ArgumentNullException(nameof(token_id));

            if (false == base.InitializeBase(logger, config_network, socket, send_event_args, recv_event_args, recv_stream))
            {
                Logger.Error($"Error in ClientUserToken.Initialize() - Fail to Initialize");
                return false;
            }

            base.TokenType = eTokenType.Client;
            base.mTokenId = token_id;

            return true;
        }
    }
}
