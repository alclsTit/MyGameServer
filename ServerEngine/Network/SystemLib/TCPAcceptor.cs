using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using ServerEngine.Common;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;
using ServerEngine.Network.Server;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// TCP Socket을 이용한 Listen 및 Accept 관련 작업 클래스 (ServerModule 당 여러개 할당가능)
    /// </summary>
    public class TcpAcceptor : NetworkSystemBase
    {
        /// <summary>
        /// listen socket that used on acceptor
        /// </summary>
        private TcpSocket? mListenSocket;

        /// <summary>
        /// Listen / Pool Config
        /// </summary>
        private IConfigListen? m_config_listen;
        private IConfigEtc? m_config_etc;

        /// <summary>
        /// Microsoft.Extensions.ObjectPool - AcceptEventArgs
        /// </summary>
        private DisposableObjectPool<SocketAsyncEventArgs>? mAcceptEventArgsPool;

        private Thread mAcceptThread;
        private AutoResetEvent mThreadBlockEvent = new AutoResetEvent(true);
        private volatile int mAcceptCount = 0;
        private volatile int mRunning = 0;

    #region property
        /// <summary>
        /// dependent on server_name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// accept run check flag
        /// </summary>
        public int Running => mRunning;

        /// <summary>
        /// Get ListenSocket
        /// </summary>
        public TcpSocket? GetListenSocket => mListenSocket;
    #endregion

        public TcpAcceptor(string name, Log.ILogger logger, ServerModule module) 
            : base(logger, module)
        {
            Name = name;    
            mAcceptThread = new Thread(() => { StartAccept(); });
        }

        #region private method
        private Socket CreateListenSocket()
        {
            if (null == m_config_listen)
                throw new NullReferenceException(nameof(m_config_listen));

            try
            {
                var socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse(m_config_listen.address), m_config_listen.port));
                socket.Listen(m_config_listen.backlog);

                return socket;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool CheckMaxConnection()
        {
            if (null == m_config_listen)
                return false;

            var current_connected_count = mAcceptCount;
            return current_connected_count <= m_config_listen.max_connection ? false : true;
        }
        #endregion

        #region public_method
        public override bool Initialize()
        {
            throw new NotSupportedException();
        }

        public override bool Initialize(IConfigListen config_listen, IConfigEtc config_etc)
        {
            if (null == config_listen) 
                throw new ArgumentNullException(nameof(config_listen));

            if (null == config_etc)
                throw new ArgumentNullException(nameof(config_etc));

            try
            {
                m_config_listen = config_listen;
                m_config_etc = config_etc;

                mListenSocket = new TcpSocket(CreateListenSocket(), base.Logger);

                var pool_default_size = m_config_etc.pools.list.FirstOrDefault(e => e.name.ToLower().Trim() == Name.ToLower().Trim())?.default_size;
                int maximum_retained = true == pool_default_size.HasValue ? pool_default_size.Value : m_config_listen.max_connection;

                mAcceptEventArgsPool = new DisposableObjectPool<SocketAsyncEventArgs>(
                    new SocketEventArgsObjectPoolPolicy(OnAcceptCompleteHandler), 
                    maximum_retained);

                var old_state = (eNetworkSystemState)mState;
                var new_state = eNetworkSystemState.Initialized;
                if (false == base.UpdateState(new_state))
                {
                    Logger.Error($"Error in TcpAcceptor.Initialize() - Fail to update state [{old_state}] -> [{new_state}]");
                    return false;
                }

                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpAcceptor Initialize Complete");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TCPListener.Initialize() - {ex.Message} - {ex.StackTrace}", ex);
                return false;
            }
        }

        public override bool Start()
        {
            if (false == CheckState(eNetworkSystemState.Initialized))
            {
                Logger.Error($"Error in TcpAcceptor.Start() - TcpAcceptor can't start when state is [{(eNetworkSystemState)mState}]");
                return false;
            }

            var new_state = eNetworkSystemState.Running;
            if (false == UpdateState(new_state))
            {
                Logger.Error($"Error in TcpAcceptor.Start() - Fail to update state [{new_state}]");
                return false;
            }

            mAcceptThread.Start();

            if (Logger.IsEnableDebug)
                Logger.Debug($"TcpAccetor Start Complete");

            return true;
        }

        public override bool Start(string address, ushort port, bool client_connect = true)
        {
            throw new NotSupportedException();
        }

        public void StartAccept()
        {
            try
            {
                if (null == mListenSocket)
                    throw new NullReferenceException(nameof(mListenSocket));

                if (null == mAcceptEventArgsPool)
                    throw new NullReferenceException(nameof(mAcceptEventArgsPool));

                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpAcceptor Accpet Start. address = {m_config_listen?.address}, port = {m_config_listen?.port}");

                while (1 == mRunning)
                {
                    mThreadBlockEvent.WaitOne();
                    
                    while (true == CheckMaxConnection())
                    {
                        // accept된 객체의 수가 현재 max_connection을 초과하게 되는 경우
                        // 기존 connection이 disconnect 될 때까지 추가 connect를 받지 않고 대기
                        Thread.Sleep(10);
                    }

                    SocketAsyncEventArgs? accept_event_args = null;
                    try
                    {
                        accept_event_args = mAcceptEventArgsPool.Get();

                        var pending = mListenSocket?.GetSocket?.AcceptAsync(accept_event_args);
                        if (false == pending)
                            OnAcceptCompleteHandler(null, accept_event_args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Exception in TcpAcceptor.StartAccept() - {ex.Message} - {ex.StackTrace}", ex);
                        if (null != accept_event_args)
                            mAcceptEventArgsPool.Return(accept_event_args);
                        mListenSocket?.DisconnectSocket();
                    }
                }

                Logger.Info($"Info in TcpAcceptor.StartAccept() - TcpAcceptor Accpet Function exit!!!");
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.StartAccept() - {ex.Message} - {ex.StackTrace}", ex);
                mListenSocket?.DisconnectSocket();
                return;
            }
        }

        public void OnAcceptCompleteHandler(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                var socket_error = e.SocketError;
                if (false == AsyncCallbackChecker.CheckCallbackHandler_SocketError(socket_error))
                {
                    Logger.Error($"Error in TcpAcceptor.OnAcceptCompleteHandler() - SocketError = {socket_error}");
                    mAcceptEventArgsPool?.Return(e);
                    return;
                }

                if (e.UserToken is null)
                {
                    Logger.Error($"Error in TcpAcceptor.OnAcceptCompleteHandler() - UserToken is null");
                    return;
                }

                // connection 완료시 UserToken 생성 및 해당 token에 대한 receive / send 로직 처리 진행
                ServerModule.OnNewClientCreateHandler(e, false);

                Interlocked.Increment(ref mAcceptCount);

                mThreadBlockEvent.Set();

                // Todo : 테스트 이후 제거 필요
                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpAcceptor Accept Complete");
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.OnAcceptCompleteHandler() - {ex.Message} - {ex.StackTrace}");
                Interlocked.Decrement(ref mAcceptCount);
            }
            finally
            {
                // 사용된 객체 반환
                mAcceptEventArgsPool?.Return(e);
            }
        }

        public override void Stop()
        {
            try
            {
                var state = (eNetworkSystemState)mState;
                if (true == base.CheckStop())
                {
                    Logger.Error($"Error in TcpAcceptor.Stop() - Stop process is already working. state = [{state}]");
                    return;
                }

                if (false == base.UpdateState(eNetworkSystemState.Stopping))
                {
                    Logger.Error($"Error in TcpAcceptor.Stop() - Fail to update state [{state}] -> [{eNetworkSystemState.Stopping}]");
                    return;
                }

                if (null == Interlocked.CompareExchange(ref mListenSocket, null, null) ||
                    true == mListenSocket?.IsNullSocket())
                {
                    return;
                }

                var old_running = mRunning;
                if (old_running != Interlocked.CompareExchange(ref mRunning, 0, 1))
                {
                    Logger.Error($"Error in TcpAcceptor.Stop() - Fail to update running flag. current running = {old_running}");
                    return;
                }

                mThreadBlockEvent.WaitOne();

                if (true == mListenSocket?.GetSocket?.Connected)
                {
                    mListenSocket?.GetSocket?.Shutdown(SocketShutdown.Both);
                    mListenSocket?.GetSocket?.Close();
                }

                mAcceptEventArgsPool?.Dispose();

                UpdateState(eNetworkSystemState.StopComplete);

                mThreadBlockEvent.Set();

            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.Stop() - {ex.Message} - {ex.StackTrace}", ex);
                return;
            }
        }
        #endregion

        /// <summary>
        /// TCPListener 멤버 데이터 초기화 
        /// * 객체 인스턴스 생성 후 가장 먼저 호출하여 멤버 초기화 진행
        /// </summary>
        /// <param name="listenInfo"></param>
        /// <param name="serverInfo"></param>
        /// <param name="logger"></param>
        /// <param name="creater"></param>
        /*public override void Initialize(ServerModuleBase module, IListenInfo listenInfo, ServerConfig serverInfo, Logger logger, Func<Session> creater)
        {
            try
            {
                base.Initialize(module, listenInfo, serverInfo, logger, creater);

                var oldState = ServerState.NotInitialized;
                if (!ChangeState(oldState, ServerState.Initialized))
                {
                    logger.Error(this.ClassName(), this.MethodName(), $"TCPListener state is [{oldState}]. TCPListener can be Initialized when state is [NotInitialized]");
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
        }*/

        /// <summary>
        /// EndPoint에 binding된 Socket으로 Listen 및 Accept 진행 (Listen -> Accept 순서대로 로직 진행)
        /// </summary>
        /// <returns></returns>
        /*public override bool Start()
        {
            var oldState = NetworkSystemState.Initialized;
            if (!ChangeState(oldState, NetworkSystemState.Running))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"TCPListener state is [{oldState}]. TCPListener can be started when state is [Initialized]");
                return false;
            }
           
            if (logger.IsDebugEnabled)
                logger.Debug("TCPListener [Accept] Started");

            StartAccept(mAcceptEvent);

            return true;
        }
        */

        /// <summary>
        /// 한 번만 진행되어야하는 Start 작업
        /// </summary>
        /// <returns></returns>
        /*public override bool StartOnce()
        {
            if (!StartListen())
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Fail to start [StartListen]");
                return false;
            }
            else
            {
                if (logger.IsDebugEnabled)
                    logger.Debug($"TCPListener [Listen({GetIPEndPoint.Address}:{GetIPEndPoint.Port})] Connected");
            }

            return true;
        }
        */

        /// <summary>
        /// Listen 진행. 한번 Binding된 ip, port는 중복으로 binding 될 수 없다
        /// </summary>
        /// <returns></returns>
        /*private bool StartListen()
        {
            var ipEndPoint = GetIPEndPoint;
            if (ipEndPoint == null)
            {
                logger.Error(this.ClassName(), this.MethodName(), "IPEndPoint is null");
                return false;
            }

            mListenSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Listen socket 옵션 세팅
            mListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            mListenSocket.NoDelay = mServerInfo.nodelay;
            mListenSocket.LingerState = new LingerOption(false, 0);

            // Listen bind 및 listen 진행
            mListenSocket.Bind(ipEndPoint);
            mListenSocket.Listen(mListenInfo.backlog);

            mAcceptEvent = new SocketAsyncEventArgs();
            mAcceptEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

            return true;
        }*/

        /*private void StartAccept(SocketAsyncEventArgs e)
        {
            try
            {
                var lPending = mListenSocket.AcceptAsync(e);
                if (!lPending)
                    OnAcceptCompleted(null, e);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }
        */

        // CallBack 핸들러와 이전 작업들은 서로 다른 스레드에서 진행된다. 공유변수가 있다면 동기화가 필요
        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (!AsyncCallbackChecker.CheckCallbackHandler_SocketError(e.SocketError))
                {
                    logger.Error(this.ClassName(), this.MethodName(), $"[SocketError = {e.SocketError}");
                    // 서버 소켓에러 발생 시, 해당 연결 Close 처리 진행
                    Stop();
                }
                else
                {
                    if (logger.IsDebugEnabled)
                        logger.Debug("Accept Completed!!!");

                    ChangeConnectState(true);

                    Session session = null;
                    var serverName = mServerModule.GetConnectedServerName(e.AcceptSocket.LocalEndPoint);
                    if (string.IsNullOrEmpty(serverName))
                    {
                        // 클라이언트가 접속했을 경우 - 접속 인원이 모두 찼는지 체크 (세션 생성 / 접속차단)
                        if (!mServerModule.mSessionManager.CheckConnectionMax())
                        {
                            lock(mLockObject)
                            {
                                session = mServerModule.NewClientSessionCreate(Guid.NewGuid().ToString(), e, logger, mSessionCreater, true);
                                if (session != null)
                                    OnSessionCreateCompleted(session, e);
                            }
                        }
                        else
                        {
                            logger.Error(this.ClassName(), this.MethodName(), $"Session[{GetIPEndPoint.Address}:{GetIPEndPoint.Port}] is Full!!!");
                            // 서버에서 관리할 수 있는 최대 클라이언트 수가 초과되었을 때, 해당 연결 Close 처리 진행
                            Stop();
                        }
                    }
                    else
                    {
                        if (!mServerModule.mSessionManager.CheckMultiConnected(e.AcceptSocket.LocalEndPoint))
                        {
                            // 서버 접속 
                            lock(mLockObject)
                            {
                                session = mServerModule.NewClientSessionCreate(serverName, e, logger, mSessionCreater, false);
                                if (session != null)
                                    OnSessionCreateCompleted(session, e);
                            }
                        }
                        else
                        {
                            // 이미 해당 IP, PORT로 소켓 연결된 서버가 존재한다. 해당 연결 Close 처리 진행
                            logger.Error(this.ClassName(), this.MethodName(), $"Session[{GetIPEndPoint.Address}:{GetIPEndPoint.Port} is already connected!!!");
                            Stop();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
                // 익셉션 발생 시, 해당 연결 Close 처리 진행
                Stop();
            }
            finally
            {
                // Socket 비동기 객체를 재사용하기 위해서 AcceptSocket null처리 및 StartAccept 재호출
                e.AcceptSocket = null;
                StartAccept(e);
            }
        }

        /// <summary>
        /// 세션 생성 후 작업
        /// 1. 세션 매니저에 세션 추가 
        /// 2. 세션 Recv 시작
        /// 3. 세션 Connected 이후 콜백메서드 호출에 따른 작업 진행
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void OnSessionCreateCompleted(Session session, SocketAsyncEventArgs e)
        {
            try
            {
                mServerModule.mSessionManager.AddSession(session);
                session.StartReceive();
                session.OnConnected(e.AcceptSocket.LocalEndPoint, session);
            }
            catch (ArgumentNullException argNullEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), argNullEx);
            }
            catch(Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }


        /// <summary>
        /// TCPListener 중지 
        /// 임의의 Task 스레드에서 호출가능하기 때문에 ThreadSafe 하게 작업
        /// </summary>
        /*public override void Stop()
        {
            if (!CheckCanStop())
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Fail to stop - TCPListener is already [{mSystemState}]");
                return;
            }
            else
            {
                if (!ChangeState(mSystemState, NetworkSystemState.Stopping))
                {
                    logger.Error($"TCPListener state is [{mSystemState}]. Fail to change state [Stopping]");
                    return;
                }
            }

            lock(mLockObject)
            {
                try
                {
                    if (mListenSocket == null)
                    {
                        logger.Error(this.ClassName(), this.MethodName(), $"Fail to stop - ListenSocket is null");
                        return;
                    }

                    //if (mListenSocket.Connected)
                    if (IsConnected)
                    {
                        mListenSocket.Shutdown(SocketShutdown.Both);
                        mListenSocket.Close();  // Socket은 Close 호출 시 내부에서 Dispose 호출
                    }

                    mAcceptEvent.Completed -= new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                    mAcceptEvent.Dispose();
                }
                catch (Exception ex)
                {
                    logger.Error(this.ClassName(), this.MethodName(), ex);
                    return;
                }
                finally
                {
                    mAcceptEvent = null;
                    mListenSocket = null;
                    mSystemState = NetworkSystemState.StopCompleted;
                }

                OnStopCallback();
            }

            ChangeConnectState(false);
        }*/
    }
}
