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

        public bool TryRemoveToken(long uid)
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
            if (0 > index || base.m_config_network.max_io_thread_count <= index)
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
                              long token_id, Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, SendStreamPool?, bool> retrieve_event)
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

            if (0 >= token_id)
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

            // UseToken 생성 후 heartbeat_start_time(sec) 이후 heartbeat check 진행
            mBackgroundTimer = new Timer(HeartbeatCheck, null, 
                                         TimeSpan.FromSeconds(config_network.config_socket.heartbeat_start_time), 
                                         TimeSpan.FromSeconds(config_network.config_socket.heartbeat_check_time));

            return true;
        }

        // ThreadPool에서 가져온 임의의 작업자 스레드(백그라운드 스레드)에서 실행
        public override void HeartbeatCheck(object? state)
        {
            try
            {
                if (Logger.IsEnableDebug)
                    Logger.Debug($"ClientUserToken [{base.mTokenId}] Heartbeat Check Start. CurTime = {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");
            
                if (0 != mHeartbeatCheckTime)
                {
                    // 클라이언트로부터 heartbeat 패킷이 서버로 전달되었고
                    // heartbeat interval이 지나서 체크되어야할 시간일 때.
                    if (mHeartbeatCheckTime == mLastHeartbeatCheckTime)
                    {
                        // 하트비트 시간이 갱신이 안되었다. 즉, 클라이언트로부터 하트비트 패킷이 제대로 전달이 되지 않았다
                        int increased = Interlocked.Increment(ref base.mHeartbeatCount);
                        if (null != base.GetConfigSocket && increased >= base.GetConfigSocket.heartbeat_count)
                        {
                            // disconnect session

                            if (Logger.IsEnableDebug)
                            {
                                var (ip, port) = GetRemoteEndPointIPAddress();
                                Logger.Debug($"ClientUserToken [{base.mTokenId}][{ip}:{port}] is disconnected. Heartbeat full check. CurTime = {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");
                            }
                        }
                    }
                    else
                    {
                        Interlocked.Exchange(ref base.mHeartbeatCount, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in ClientUserToken.HeartbeatCheck() - {ex.Message} - {ex.StackTrace}");
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
