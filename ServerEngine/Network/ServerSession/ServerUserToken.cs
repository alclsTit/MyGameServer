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

            // UseToken 생성 후 heartbeat_start_time(sec) 이후 heartbeat check 진행
            mBackgroundTimer = new Timer(HeartbeatCheck, null,
                                         TimeSpan.FromSeconds(config_network.config_socket.heartbeat_start_time),
                                         TimeSpan.FromSeconds(config_network.config_socket.heartbeat_check_time));


            return true;
        }

        // ThreadPool에서 가져온 임의의 작업자 스레드(백그라운드 스레드)에서 실행
        // ClientUserToken과 ServerUserToken이 내부 로직에 차이가 있을 수 있어 일단 함수 이원화
        public override void HeartbeatCheck(object? state)
        {
            try
            {
                if (Logger.IsEnableDebug)
                    Logger.Debug($"ServerUserToken [{base.mTokenId}] Heartbeat Check Start. CurTime = {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");
            
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
                                Logger.Debug($"ServerUserToken [{base.mTokenId}][{ip}:{port}] is disconnected. Heartbeat full check. CurTime = {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}");
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
                Logger.Error($"Exception in ServerUserToken.HeartbeatCheck() - {ex.Message} - {ex.StackTrace}");
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
