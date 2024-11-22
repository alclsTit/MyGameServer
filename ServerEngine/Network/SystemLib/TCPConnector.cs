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
    public class TcpConnector : NetworkSystemBase
    {
        private TcpSocket? mClientSocket;
        private IPEndPoint? mRemoteEndpoint;

        #region property
        public string Name { get; private set; }
        public SocketAsyncEventArgs? ConnectEventArgs { get; private set; }
        public TcpSocket? GetClientSocket => mClientSocket;
        public IPEndPoint? GetRemoteEndPoint => mRemoteEndpoint;
        #endregion

        public TcpConnector(string name, Log.ILogger logger, ServerModule module)
            : base(logger, module)
        {
            Name = name;
            mType = eNetworkSystemType.Connect;
        }

        #region public_method
        public override bool Initialize()
        {
            try
            {
                mClientSocket = new TcpSocket(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), base.Logger);

                UpdateState(eNetworkSystemState.Initialized);

                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpConnector Initialize Complete");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpConnector.Initialize() - {ex.Message} - {ex.StackTrace}", ex);
                return false;
            }
        }

        public override bool Initialize(IConfigListen config_listen, IConfigEtc config_etc)
        {
            throw new NotSupportedException();
        }

        public override bool Start()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// request connect to server
        /// </summary>
        /// <param name="address">ip_address</param>
        /// <param name="port">port</param>
        /// <param name="client_connect">boolean that defines client or server connection</param>
        /// <exception cref="ArgumentNullException"></exception>
        public override bool Start(string address, ushort port, bool client_connect = true)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException(nameof(address));

            if (port < 0)
                throw new ArgumentNullException (nameof(port));

            if (null == mClientSocket)
                throw new NullReferenceException(nameof(mClientSocket));

            try
            {
                if (false == CheckState(eNetworkSystemState.Initialized) && 
                    false == CheckState(eNetworkSystemState.StopComplete))
                {
                    Logger.Error($"Error in TcpConnector.Start() - TcpConnector Can' Start Connect. state = {(eNetworkSystemState)mState}");
                    return false;
                }

                ConnectEventArgs = new SocketAsyncEventArgs();
                ConnectEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnectCompleteHandler);
                ConnectEventArgs.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(address), port);
                ConnectEventArgs.UserToken = true == client_connect ? new ClientUserToken() : new ServerUserToken();

                var pending = mClientSocket.GetSocket?.ConnectAsync(ConnectEventArgs);
                if (false == pending)
                    OnConnectCompleteHandler(null, ConnectEventArgs);

                UpdateState(eNetworkSystemState.Running);

                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpConnector Start Complete");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpConnector.StartConnect() - {ex.Message} - {ex.StackTrace}", ex);
                return false;
            }
        }

        public void OnConnectCompleteHandler(object? sender, SocketAsyncEventArgs e)
        {
            var socket_error = e.SocketError;
            if (false == AsyncCallbackChecker.CheckCallbackHandler_SocketError(socket_error))
            {
                Logger.Error($"Error in TcpConnector.OnConnectCompleteHandler() - SocketError = {socket_error}");
                return;
            }

            if (e.UserToken is null)
            {
                Logger.Error($"Error in TcpConnector.OnConnectCompleteHandler() - UserToken is null");
                return;
            }

            try
            {
                Interlocked.Exchange(ref mRemoteEndpoint, (IPEndPoint?)e.RemoteEndPoint);
                if (null == mRemoteEndpoint)
                    Logger.Error($"Error in TcpConnector.OnConnectCompleteHandler() - RemoteEndPoint is null");

                // connection 완료시 UserToken 생성 및 해당 token에 대한 receive / send 로직 처리 진행
                if (ServerModule.TryGetTarget(out var module))
                {
                    module.OnNewClientCreateHandler(e, true);
                }
                else
                {
                    Logger.Error($"Error in TcpConnector.OnConnectCompleteHandler() - TcpConnector's ServerModule was disposed. Handler Call Error");
                }

                if (Logger.IsEnableDebug)
                    Logger.Debug($"Debug in TcpConnector.OnConnectCompleteHandler() - [{Name}][{mRemoteEndpoint?.Address}:{mRemoteEndpoint?.Port}] Server join Complete");
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpConnector.OnConnectCompleteHandler() - {ex.Message} - {ex.StackTrace}", ex);
                return;
            }
        }

        public override void Stop()
        {
            try
            {
                var state = (eNetworkSystemState)mState;
                if (CheckStop())
                {
                    Logger.Error($"Error in TcpConnector.Stop() - Stop process is already working. state = [{state}]");
                    return;
                }

                if (null == Interlocked.CompareExchange(ref mClientSocket, null, null) || 
                    true == mClientSocket?.IsNullSocket())
                {
                    return;
                }

                UpdateState(eNetworkSystemState.Stopping);

                if (true == mClientSocket?.GetSocket?.Connected)
                    mClientSocket.Dispose(SocketShutdown.Send);

                ConnectEventArgs?.Dispose();

                UpdateState(eNetworkSystemState.StopComplete);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpConnector.Stop() - {ex.Message} - {ex.StackTrace}", ex);
                return;
            }
        }
        #endregion

        /*
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
                    // 2022.05.14 세션 매니저 수정 작업 - 세션 추가, 삭제를 이곳으로 옮겨서 진행할 수 있는지...
                    //var session = mServerModule.mSessionManager.
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
        */


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
