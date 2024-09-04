using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;

using ServerEngine.Common;
using ServerEngine.Log;
using ServerEngine.Network.SystemLib;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;
using System.Collections.Frozen;
using Microsoft.Extensions.ObjectPool;

namespace ServerEngine.Network.Server
{
    public abstract class ServerModuleBase : IServerModule, IAsyncEventCallbackHandler
    {
        public enum eServerState : int
        {
            None = 0,
            Initialized = 1,
            SetupFinished = 2,
            NotStarted = 3,
            Running = 4,
            Stopped = 5,
            Max = 6
        }

        /// <summary>
        /// server_module state
        /// </summary>
        private volatile int mState = (int)eServerState.None;

        /// <summary>
        /// Server Module endpoint info
        /// </summary>
        private IPEndPoint mLocalEndPoint;

        /// <summary>
        /// Microsoft.Extensions.ObjectPool
        /// </summary>
        protected DisposableObjectPool<SocketAsyncEventArgs> mSendEventArgsPool, mRecvEventArgsPool;

        private DisposableObjectPool<ClientUserToken> mClientUserTokenPool;

        #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
        /*
        public SocketAsyncEventArgsPool mRecvEventPool { get; private set; }
        public SocketAsyncEventArgsPool mSendEventPool { get; private set; }
        */
        #endregion

        /// <summary>
        /// 서버 세션 매니저
        /// </summary>
        public ISessionManager mSessionManager { get; private set; }

        /// <summary>
        /// 서버모듈이 가지고 있는 Accept / Connect 객체 (1:1 관계)
        /// </summary>
        public INetworkSystemBase mNetworkSystem { get; private set; }

        public ServerConfig mServerInfo { get; private set; }

        /// <summary>
        /// 해당 서버모듈에 연관된 리슨정보
        /// </summary>
        public IConfigListen config_listen { get; private set; }

        /// <summary>
        /// 모든 리슨정보
        /// </summary>
        public List<IConfigListen> config_listen_list { get; private set; }

     
        /// <summary>
        /// 각각의 서버모듈이 작동하기 시작한 시작시간 (utc+0)
        /// </summary>
        public DateTime mStartTime { get; protected set; }

        /// <summary>
        /// 이전 서버모듈이 작동을 시작한 시간 (utc+0)
        /// </summary>
        public DateTime mLastActiveTime { get; protected set; }

    #region property
        /// <summary>
        /// server_module name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// server_module logger
        /// </summary>
        public Log.ILogger Logger { get; }

        /// <summary>
        /// server_module config
        /// </summary>
        protected IConfigCommon Config { get; private set; }

        /// <summary>
        /// Manage Client Access UserToken
        /// </summary>
        public ClientUserTokenManager ClientUserTokenManager { get; private set; }

        /// <summary>
        /// Get ServerModule State
        /// </summary>
        public int GetState => mState;

        /// <summary>
        /// Get Server Module ip_address and port
        /// </summary>
        public IPEndPoint GetLocalEndPoint => mLocalEndPoint;
    #endregion

        protected ServerModuleBase(string name, Log.ILogger logger, IConfigCommon config, IAsyncEventCallbackHandler.AsyncEventCallbackHandler handler)
        {
            this.Name = name;
            this.Logger = logger;

            mStartTime = DateTime.UtcNow;
            mLastActiveTime = mStartTime;

            // Microsoft.Extensions.ObjectPool 사용
            // ObjectPool에서 관리하는 최대 풀 객체 수 세팅
            int maximum_retained = 0;
            var pool_default_size = config.config_etc.pools.list.FirstOrDefault(e => e.name.ToLower().Trim() == name.ToLower().Trim())?.default_size;
            
            foreach(var item in config.config_network.config_listen_list)
                maximum_retained += item.max_connection;

            maximum_retained = true == pool_default_size.HasValue ? pool_default_size.Value : maximum_retained;

            if (Logger.IsEnableDebug)
                Logger.Debug($"Debug in ServerModuleBase() - SocketAsyncEventArgs ObjectPool Size = {maximum_retained}");

            // ObjectPool 객체에 callback handler만 선제적으로 등록
            mSendEventArgsPool = new DisposableObjectPool<SocketAsyncEventArgs>(new SocketEventArgsObjectPoolPolicy(handler), maximum_retained);
            mRecvEventArgsPool = new DisposableObjectPool<SocketAsyncEventArgs>(new SocketEventArgsObjectPoolPolicy(handler), maximum_retained);

            mClientUserTokenPool = new DisposableObjectPool<ClientUserToken>(new DefaultPooledObjectPolicy<ClientUserToken>(), maximum_retained);

            // Client에서 접속해서 생성된 UserToken 관리
            ClientUserTokenManager = new ClientUserTokenManager(logger, config.config_network);
        }

        public bool UpdateState(eServerState state)
        {
            var old_state = mState;
            if (old_state == (int)state)
                return true;

            return old_state == Interlocked.Exchange(ref mState, (int)state) ? true : false;
        }

        public virtual bool Initialize()
        {
            var config_listen = Config.config_network.config_listen_list.FirstOrDefault(e => e.name.ToLower().Trim() == Name.ToLower().Trim());
            if (null == config_listen)
            {
                Logger.Error($"Error in ServerModuleBase.Initialize() - IConfigListen is null");
                return false;
            }
            
            mLocalEndPoint = ServerHostFinder.GetServerIPAddress(config_listen.port);

            var old_state = (eServerState)mState;
            var new_state = eServerState.Initialized;
            if (false == UpdateState(new_state))
            {
                Logger.Error($"Error in ServerModuleBase.Initialize() - Fail to Update State [{old_state}] -> [{new_state}]");
                return false;
            }

            return true;
        }

        public virtual void Initialize(List<IConfigListen> listen_config_list, IConfigListen config_listen, ServerConfig serverInfo, INetworkSystemBase networkSystem, Log.ILogger logger, Func<Session> creater)
        {
            if (null == listen_config_list)
                throw new ArgumentNullException(nameof(listen_config_list));

            if (config_listen == null)
                throw new ArgumentNullException(nameof(config_listen));

            if (serverInfo == null)
                throw new ArgumentNullException(nameof(serverInfo));

            if (networkSystem == null)
                throw new ArgumentNullException(nameof(networkSystem));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (creater == null)
                throw new ArgumentNullException(nameof(creater));

            mListenInfoList = listenInfoList;
            mListenInfo = config_listen;
            ipEndPoint = ServerHostFinder.GetServerIPAddress(config_listen.port);

            mServerInfo = serverInfo;
            
            mNetworkSystem = networkSystem;

            #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
            /*
            mRecvEventPool = new SocketAsyncEventArgsPool(mServerInfo.maxConnectNumber);
            mSendEventPool = new SocketAsyncEventArgsPool(mServerInfo.maxConnectNumber);
            */
            #endregion

            var old_state = mState;
            if (false == UpdateState(eServerState.Initialized))
            {
                logger.Error($"Error in ServerModuleBase.Initialize() - Fail to Update State [{(eServerState)old_state}] -> [{(eServerState)mState}]");
                return;
            }

            // 마지막에 초기화 진행
            mNetworkSystem.Initialize(this, config_listen, serverInfo, this.Logger, creater);
            mNetworkSystem.StopCallback += OnNetworkSystemStopped;
        }

        public void InitializeSessionManager(ISessionManager sessionManager)
        {
            if (sessionManager == null)
                throw new ArgumentNullException(nameof(sessionManager));

            mSessionManager = sessionManager;
            mSessionManager.Initialize(mServerInfo.maxConnectNumber);
        }

        public virtual bool Start()
        {
            if (mNetworkSystem.Start())
            {
                return true;
            }
            else
            {
                mNetworkSystem.Stop();        
                return false;
            }
        }

        public virtual bool StartOnce()
        {
            if (mNetworkSystem.StartOnce())
                return true;

            return false;
        }

        public virtual void Stop()
        {
            mNetworkSystem.Stop();
        }

        public virtual void OnNewClientCreateHandler(SocketAsyncEventArgs e)
        {
            // accept 및 connect callback handler에서 해당 메서드를 호출
            // socketasynceventargs를 사용하면 비동기 호출한 대상의 usertoken은 알 수 있음
            // 다만, 현재 서버에서 관리중인 전체 usertoken은 알 방법이 없음. usertoken은 session 객체에 포함될 네트워크 기능을 포함하고 있는 멤버객체가 될 예정 

            // UserToken 을 클라이언트 / 서버용 따로관리
            var last_operation = e.LastOperation;
            if (SocketAsyncOperation.Connect != last_operation || SocketAsyncOperation.Accept != last_operation)
            {
                Logger.Error($"Error in TcpAcceptor.OnNewClientCreateHandler() - SocketAsyncOperation Error. LastOperation = {last_operation}");
                return;
            }

            if (e.UserToken?.GetType() == typeof(ClientUserToken))
            {
                // connection request by client
                var token = e.UserToken as ClientUserToken;
                if (null == token)
                {
                    Logger.Error($"Error in TcpAcceptor.OnNewClientCreateHandler() - Fail to UserToken Casting [ClientUserToken]");
                    return;
                }

                if (null == e.ConnectSocket)
                {
                    Logger.Error($"Error in TcpAcceptor.OnNewClientCreateHandler() - ConnectSocket is null");
                    return;
                }

                var client_token = mClientUserTokenPool.Get();
                client_token.Initialize(Logger, Config.config_network, new TcpSocket(e.ConnectSocket, Logger), UserToken.eTokenType.Client, mSendEventArgsPool.Get(), mRecvEventArgsPool.Get());

                ClientUserTokenManager.TryAddUserToken();

            }
            else
            {
                // connection request by server 
                var token = e.UserToken as ServerUserToken;
                if (null == token)
                {
                    Logger.Error($"Error in TcpAcceptor.OnNewClientCreateHandler() - Fail to UserToken Casting [ServerUserToken]");
                    return;
                }
            }



        }

        public virtual Session NewClientSessionCreate(string sessionID, SocketAsyncEventArgs e, Logger logger, Func<Session> creater, bool isClient)
        {
            if (e.LastOperation != SocketAsyncOperation.Accept && 
                e.LastOperation != SocketAsyncOperation.Connect)
            {
                logger.Error(this.ClassName(), this.MethodName(), "Socket LastOperation must be [Accept] or [Connect]");
                return null;
            }

            var session = creater.Invoke();
            //var socket = e.LastOperation == SocketAsyncOperation.Accept ? e.AcceptSocket : e.ConnectSocket;
            if (e.LastOperation == SocketAsyncOperation.Accept)
                session.Initialize(sessionID, e.AcceptSocket, mServerInfo, logger, mListenInfoList, isClient);
            else
                session.Initialize(sessionID, e.ConnectSocket, mServerInfo, logger, mListenInfoList, isClient);

            //session.Initialize(sessionID, socket, mServerInfo, logger, mListenInfoList, isClient);

            #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
            /*
            if (mRecvEventPool.IsEmpty)
            {
                if (logger.IsWarnEnabled)
                    logger.Warn(this.ClassName(), this.MethodName(), "[Recv] SocketAsyncEventArgsPool is empty. Increase pool max count");
            }

            if (mSendEventPool.IsEmpty)
            {
                if (logger.IsWarnEnabled)
                    logger.Warn(this.ClassName(), this.MethodName(), "[Send] SocketAsyncEventArgsPool is empty. Increase pool max count");
            }
            */
            #endregion

            session.Closed += OnSessionClosed;

            #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
            /*
            session.SetRecvEventByPool(mRecvEventPool.Pop());
            session.SetSendEventByPool(mSendEventPool.Pop());
            */
            #endregion
            session.SetSendEventByPool(mSendEventArgsPool.Get());
            session.SetRecvEventByPool(mRecvEventArgsPool.Get());

            return session;
        }
         
        public virtual void OnSessionClosed(Session session, eCloseReason reason)
        {
            if (null == session)
            {
                Logger.Error($"Error in ServerModuleBase.OnSessionClosed() - Session is null!!!");
                return;
            }

            session.Closed -= OnSessionClosed;

            session.ClearAllSocketAsyncEvent();

            #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
            /*
            mRecvEventPool.Push(session.mRecvEvent);
            mSendEventPool.Push(session.mSendEvent);
            */
            #endregion
            mSendEventArgsPool.Return(session.mSendEvent);
            mRecvEventArgsPool.Return(session.mRecvEvent);

            mSessionManager.Close(session.mSessionID);
        }

        private void OnNetworkSystemStopped(object? sender, EventArgs e)
        {
            if (Logger.IsEnableDebug)
                Logger.Debug($"[{this.Name}][{ipEndPoint.Address}:{ipEndPoint.Port}] was stopped");
        }

        public string GetConnectedServerName(EndPoint endPoint)
        {
            var ipEndPoint = (IPEndPoint)endPoint;
            var listenInfo = mListenInfoList.Find((item) => { return item.port == ipEndPoint.Port; });
            return listenInfo != null ? Name : default(string);    
        }

    }
}
