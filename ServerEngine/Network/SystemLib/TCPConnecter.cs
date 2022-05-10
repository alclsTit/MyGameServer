using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

using ServerEngine.Common;
using ServerEngine.Network.Server;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;
using ServerEngine.Log;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// TCP Socket을 이용한 Connect 관련 작업 클래스
    /// </summary>
    public class TCPConnecter : NetworkSystemBase
    {
        private static object mLockObject = new();

        /// <summary>
        /// TCPConnecter 멤버 데이터 초기화
        /// * 객체 인스턴스 생성 후 가장 먼저 호출하여 멤버 초기화 진행
        /// </summary>
        /// <param name="listenInfo"></param>
        /// <param name="serverInfo"></param>
        /// <param name="logger"></param>
        /// <param name="creater"></param>
        public override void Initialize(ServerModuleBase module, IListenInfo listenInfo, ServerConfig serverInfo, Logger logger, Func<Session> creater)
        {
            try
            {
                base.Initialize(module, listenInfo, serverInfo, logger, creater);

                var oldState = ServerState.NotInitialized;
                if (!ChangeState(oldState, ServerState.Initialized))
                {
                    logger.Error(this.ClassName(), this.MethodName(), $"TCPConnecter state is [{oldState}]. TCPConnecter can be Initialized when state is [NotInitialized]");
                    return;
                }
            }
            catch (ArgumentNullException argNullEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), argNullEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }

        /// <summary>
        /// ListenInfo의 IPEndPoint에 대한 Connect 진행 요청 메서드 
        /// </summary>
        /// <returns></returns>
        public override bool Start()
        {
            var lIPEndPoint = GetIPEndPoint;
            if (lIPEndPoint == null)
            {
                logger.Error(this.ClassName(), this.MethodName(), "IPEndPoint is null");
                return false;
            }

            Socket clientSocket = new Socket(lIPEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            SocketAsyncEventArgs connectEvent = new SocketAsyncEventArgs();
            connectEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectCompleted);
            // 해당 클라이언트가 연결할 서버 정보(IP, PORT)
            connectEvent.RemoteEndPoint = lIPEndPoint;
            connectEvent.UserToken = clientSocket;

            var oldState = NetworkSystemState.Initialized;
            if (!ChangeState(oldState, NetworkSystemState.Running))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"TCPConnecter state is [{oldState}]. TCPConnecter can be started when state is [Initialized]");
                return false;
            }

            if (logger.IsDebugEnabled)
                logger.Debug($"TCPConnecter [{lIPEndPoint.Address.ToString()}:{lIPEndPoint.Port}] Connected");

            StartConnect(connectEvent);

            return true;
        }

        public override bool StartOnce()
        {
            return true;
        }

        private void StartConnect(SocketAsyncEventArgs e)
        {
            try
            {
                Socket socket = e.UserToken as Socket;
                if (socket != null)
                {
                    bool lPending = socket.ConnectAsync(e);
                    if (!lPending)
                        OnConnectCompleted(null, e);
                }
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (!AsyncCallbackChecker.CheckCallbackHandler_SocketError(e.SocketError))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"[SocketError = {e.SocketError}]");
                // 서버 소켓에러 발생 시, 해당 연결 Close 처리진행
                Stop();
                return;
            }

            try
            {
                if (logger.IsDebugEnabled)
                    logger.Debug("Connect Completed!!!");

                ChangeConnectState(true);

                lock(mLockObject)
                {
                    var session = mServerModule.NewClientSessionCreate(Guid.NewGuid().ToString(), e, logger, mSessionCreater, true);
                    if (session != null)
                    {
                        session.StartReceive();
                        session.OnConnected(e.RemoteEndPoint);
                    }
                }

                //StartConnect(e);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
                // 익셉션 발생 시, 해당 연결 Close 처리 진행
                Stop();
            }
        }

        public override void Stop()
        {
            if (!CheckCanStop())
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Fail to stop - TCPConnecter is already [{mSystemState}]");
                return;
            }
            else
            {
                if (!ChangeState(mSystemState, NetworkSystemState.Stopping))
                {
                    logger.Error($"TCPConnecter state is [{mSystemState}]. Fail to change state [Stopping]");
                    return;
                }
            }

            // Todo: 이후 connect 관련 작업 중단 시 추가될 부분
            ChangeConnectState(false);

            Interlocked.Exchange(ref mSystemState, NetworkSystemState.StopCompleted);
        }

        #region "ConnectBackup"
        /*public class TCPConnector : NetworkSystemBase
        {     
            public TCPConnector(IListenInfo listenInfo, IServerInfo serverInfo, IAppServer appServer, Func<Session> sessionCreater) 
                : base(listenInfo, serverInfo, appServer, sessionCreater)
            {
            }

            public override bool Start()
            {
                if (!TryConnect(out Socket clientSocket))
                    return false;

                SocketAsyncEventArgs connectEvent = new SocketAsyncEventArgs();
                connectEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectCompleted);
                connectEvent.RemoteEndPoint = GetIPEndPoint;
                connectEvent.UserToken = clientSocket;

                StartConnect(connectEvent);

                if (!ChangeState(mSystemState, NetworkSystemState.Running))
                {
                    logger.Error(this.ClassName(), this.MethodName(), $"Fail to change NetworkSystemState[{mSystemState}]");
                    return false;
                }

                if (logger.IsDebugEnabled)
                    logger.Debug("Connect Start!!!");

                return true; 
            }

            private bool TryConnect(out Socket clientSocket)
            {
                var IEndPoint = GetIPEndPoint;
                if (IEndPoint == null)
                {
                    clientSocket = null;
                    return false;
                }

                //clientSocket = new Socket(mListenInfo.ipEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                //return true;
            }

            private void StartConnect(SocketAsyncEventArgs e)
            {
                using(var clientSocket = e.UserToken as Socket)
                {
                    bool lPending = clientSocket.ConnectAsync(e);
                    if (!lPending)
                        OnConnectCompleted(null, e);
                }
            }

            private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
            {
                if (!AsyncCallbackChecker.CheckCallbackHandler_SocketError(e.SocketError))
                {
                    logger.Error(this.ClassName(), this.MethodName(), $"[SocketError = {e.SocketError}");
                    return;
                }

                try
                {
                    if (logger.IsDebugEnabled)
                        logger.Debug("Connect Completed!!!");

                    var session = mSessionCreater.Invoke();

                }
                catch (Exception ex)
                {
                    logger.Error(this.ClassName(), this.MethodName(), ex);
                }
            }

            /// <summary>
            /// TCPConnector 중지 
            /// 임의의 Task 스레드에서 호출가능하기 때문에 ThreadSafe 하게 작업
            /// </summary>
            public override void Stop()
            {
                if (!CheckCanStop())
                {
                    logger.Error(this.ClassName(), this.MethodName(), $"Fail to stop - NetworkSystemState is already [{mSystemState}]");
                    return;
                }
                else
                {
                    Interlocked.Exchange(ref mSystemState, NetworkSystemState.Stopping);
                }

                // Todo: 이후 connect 관련 작업 중단 시 추가될 부분 

                Interlocked.Exchange(ref mSystemState, NetworkSystemState.StopCompleted);
            }

        }
        */
        #endregion
    }
}
