using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using ServerEngine.Common;
using ServerEngine.Network.Server;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;
using ServerEngine.Log;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// TCP Socket을 이용한 Listen 및 Accept 관련 작업 클래스
    /// </summary>
    public class TcpAcceptor : NetworkSystemBase
    {
        /// <summary>
        /// dependent on server_name
        /// </summary>
        public string Name { get; private set; }
       
        /// <summary>
        /// listen socket that used on acceptor
        /// </summary>
        public TcpSocket? mListenSocket { get; private set; }

        /// <summary>
        /// Socket 통신용 비동기 객체 선언 
        /// </summary>
        private SocketAsyncEventArgs mAcceptEvent;

        /// <summary>
        /// Thread 동기화에 사용될 Lock 객체(CriticalSection)
        /// </summary>

        private readonly object mLockObject = new Object();

        /// <summary>
        /// Listen / Pool Config
        /// </summary>
        private IConfigListen m_listen_config;
        private IConfigEtc m_config_etc;

        /// <summary>
        /// Microsoft.Extensions.ObjectPool - AcceptEventArgs
        /// </summary>
        #region Microsoft.Extensions.ObjectPool
        private Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider mSocketEventArgsPoolProvider;
        private SocketEventArgsObjectPoolPolicy SocketEventArgsPoolPolicy;

        public Microsoft.Extensions.ObjectPool.ObjectPool<SocketAsyncEventArgs> mAcceptEventArgsPool;
        #endregion

        private Thread mAcceptThread;
        private AutoResetEvent mThreadBlockEvent = new AutoResetEvent(false);
        private volatile int mAcceptCount;

        public TcpAcceptor(string name, Log.Logger logger, IConfigListen listen_config, IConfigEtc config_etc) 
            : base()
        {
            Name = name;    
            m_listen_config = listen_config;
            m_config_etc = config_etc;

            mAcceptThread = new Thread(() => { StartAccept(); });
        }

        private Socket CreateListenSocket()
        {
            var socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);

            try
            {
                socket.Bind(new IPEndPoint(IPAddress.Parse(m_listen_config.address), m_listen_config.port));
                socket.Listen(m_listen_config.backlog);

                return socket;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void OnAcceptCompleteHandler(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                mThreadBlockEvent.Set();
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.OnAcceptCompleteHandler() - {ex.Message} - {ex.StackTrace}");
                mAcceptEventArgsPool.Return(e);
            }
        }

        public bool Initialize()
        {
            try
            {
                base.Initialize();
                
                mListenSocket = new TcpSocket(CreateListenSocket(), base.Logger);

                mSocketEventArgsPoolProvider = new Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider();
                var pool_default_size = m_config_etc.pools.list.FirstOrDefault(e => e.name.ToLower().Trim() == Name.ToLower().Trim())?.default_size;
                if (true == pool_default_size.HasValue)
                    mSocketEventArgsPoolProvider.MaximumRetained = pool_default_size.Value;
                else
                    mSocketEventArgsPoolProvider.MaximumRetained = Environment.ProcessorCount * 2;

                mAcceptEventArgsPool = mSocketEventArgsPoolProvider.Create(new SocketEventArgsObjectPoolPolicy(OnAcceptCompleteHandler));

                var old_state = ServerState.NotInitialized;
                if (false == UpdateState(eNetworkSystemState.Initialized))
                {
                    Logger.Error($"Error in TcpAcceptor.Initialize() - Fail to update state [{old_state}] -> [{(eNetworkSystemState)mState}]");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TCPListener.Initialize() - {ex.Message} - {ex.StackTrace}", ex);
                return false;
            }
        }

        public void StartAccept()
        {
            if (null == mListenSocket)
                throw new NullReferenceException(nameof(mListenSocket));

            try
            {
                mThreadBlockEvent.WaitOne();

                SocketAsyncEventArgs accept_event_args = mAcceptEventArgsPool.Get();

                var pending = mListenSocket.GetSocket?.AcceptAsync(accept_event_args);
                if (false == pending)
                    OnAcceptCompleteHandler(null, accept_event_args);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.StartAccept() - {ex.Message} - {ex.StackTrace}");
            }       
        }

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

        /// <summary>
        /// 한 번만 진행되어야하는 Start 작업
        /// </summary>
        /// <returns></returns>
        public override bool StartOnce()
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
        public override void Stop()
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
        }    
    }
}
