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
using System.Security.Permissions;
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
        public ConcurrentDictionary<string, ClientUserToken> UserTokens { get; private set; } = new ConcurrentDictionary<string, ClientUserToken>();

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
                int max_send_thread_count = m_config_network.max_send_thread_count;
                int user_per_thread = max_connection / max_send_thread_count;
                for (var i = 0; i < max_send_thread_count; ++i)
                    mThreadUserTokens.TryAdd(i, new List<ClientUserToken>(user_per_thread));

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in ClientUserTokenManager.Initialize() - {ex.Message} - {ex.StackTrace}", ex);
                return false;
            }
        }

        // UID의 맨 끝에 해당하는 전역카운터의 일의 자리 숫자가 index로 설정됨
        // - index는 UIDGenerator에서 eContentsType:UserToken에 해당하는 mLoopLimit의 범위내에서 생성됨
        public bool TryAddUserToken(string uid, ClientUserToken token)
        {
            if (string.IsNullOrEmpty(uid))
                throw new ArgumentException(nameof(uid));

            if (null == token)
                throw new ArgumentNullException(nameof(token));

            if (false == int.TryParse(uid[0].ToString(), out var uid_first_index))
                throw new ArgumentNullException(nameof(uid));

            // uid의 일의 자리 값을 꺼낸다
            // ex: max_send_thread_count가 4라면, 1 >> 1, 5 >> 1, 9 >> 1 
            int index = uid_first_index % m_config_network.max_send_thread_count;
            mThreadUserTokens[index].Add(token);

            return UserTokens.TryAdd(uid, token);
        }

        public bool TryRemoveToken(string uid)
        {
            if (UserTokens.TryGetValue(uid, out var token))
            {
                token.Dispose();

                return true;
            }

            return false;
        }

        public async ValueTask Run(int index)
        {
            if (0 > index || m_config_network.max_send_thread_count < index)
                throw new ArgumentException($"Index {index}");

            while (true)
            {
                // 해당 Thread에 엮인 모든 UserToken에 대해서 SendProcess 진행
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
        private bool mDisposed = false;
        public ClientUserToken() : base() { }

        public bool Initialize(Log.ILogger logger, IConfigNetwork config_network, SocketBase socket, 
                              SocketAsyncEventArgs send_event_args, SocketAsyncEventArgs recv_event_args, 
                              SendStreamPool? send_stream_pool, RecvStream recv_stream, 
                              string token_id, Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, SendStreamPool?, bool> retrieve_event)
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
                throw new ArgumentNullException (nameof(send_stream_pool));  

            if (null == recv_stream)
                throw new ArgumentNullException(nameof(recv_stream));

            if (string.IsNullOrEmpty(token_id))
                throw new ArgumentNullException(nameof(token_id));

            if (null == retrieve_event)
                throw new ArgumentNullException(nameof(retrieve_event));

            if (false == base.InitializeBase(logger, config_network, socket, 
                                             send_event_args, recv_event_args, 
                                             send_stream_pool, recv_stream, retrieve_event))
            {
                Logger.Error($"Error in ClientUserToken.Initialize() - Fail to Initialize");
                return false;
            }

            base.TokenType = eTokenType.Client;
            base.mTokenId = token_id;

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
                Logger.Error($"Exception in ClientUserToken.HeartbeatCheck() - UserToken >> [{TokenType}:{mTokenId}]. {ex.Message} - {ex.StackTrace}");
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
