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
        /// server_module name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// server_module state
        /// </summary>
        private volatile int mState = (int)eServerState.None;

        /// <summary>
        /// server_module logger
        /// </summary>
        public Log.ILogger Logger { get; }

        #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
        /*
        public SocketAsyncEventArgsPool mRecvEventPool { get; private set; }
        public SocketAsyncEventArgsPool mSendEventPool { get; private set; }
        */
        #endregion

        #region "Microsoft.Extensions.ObjectPool"
        private Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider mSocketEventArgsPoolProvider;
        private SocketEventArgsObjectPoolPolicy SocketEventArgsPoolPolicy;
        public Microsoft.Extensions.ObjectPool.ObjectPool<SocketAsyncEventArgs> mSendEventArgsPool { get; private set; }
        public Microsoft.Extensions.ObjectPool.ObjectPool<SocketAsyncEventArgs> mRecvEventArgsPool { get; private set; }
        #endregion

        /// <summary>
        /// server_module config
        /// </summary>
        protected IConfigCommon Config { get; private set; }

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
        public IConfigListen mListenInfo { get; private set; }

        /// <summary>
        /// 모든 리슨정보
        /// </summary>
        public List<IConfigListen> mListenInfoList { get; private set; }

        public IPEndPoint ipEndPoint { get; private set; }

        /// <summary>
        /// 각각의 서버모듈이 작동하기 시작한 시작시간 (utc+0)
        /// </summary>
        public DateTime mStartTime { get; protected set; }

        /// <summary>
        /// 이전 서버모듈이 작동을 시작한 시간 (utc+0)
        /// </summary>
        public DateTime mLastActiveTime { get; protected set; }

        public int GetServerState => mState;

        protected ServerModuleBase(string name, Log.ILogger logger, IConfigCommon config, IAsyncEventCallbackHandler.AsyncEventCallbackHandler handler)
        {
            this.Name = name;
            this.Logger = logger;

            mStartTime = DateTime.UtcNow;
            mLastActiveTime = mStartTime;

            // Microsoft.Extensions.ObjectPool 사용
            // ObjectPool에서 관리하는 최대 풀 객체 수 세팅
            mSocketEventArgsPoolProvider = new Microsoft.Extensions.ObjectPool.DefaultObjectPoolProvider();
            var pool_default_size = config.config_etc.pools.list.FirstOrDefault(e => e.name.ToLower().Trim() == name.ToLower().Trim())?.default_size;
            if (true == pool_default_size.HasValue)
                mSocketEventArgsPoolProvider.MaximumRetained = pool_default_size.Value;
            else
                mSocketEventArgsPoolProvider.MaximumRetained = Environment.ProcessorCount * 2; 

            if (Logger.IsEnableDebug)
                Logger.Debug($"Debug in ServerModuleBase() - SocketAsyncEventArgs ObjectPool Size = {mSocketEventArgsPoolProvider.MaximumRetained}");

            // ObjectPool 객체에 callback handler만 선제적으로 등록
            mSendEventArgsPool = mSocketEventArgsPoolProvider.Create(new SocketEventArgsObjectPoolPolicy(handler));
            mRecvEventArgsPool = mSocketEventArgsPoolProvider.Create(new SocketEventArgsObjectPoolPolicy(handler));
        }

        public bool UpdateState(eServerState state)
        {
            var old_state = mState;
            if (old_state == (int)state)
                return true;

            return old_state == Interlocked.Exchange(ref mState, (int)state) ? true : false;
        }

        // 삭제예정
        /*public bool ChangeState(int oldState, int newState)
        {
            var curState = mState;
            if (curState == newState)
                return true;

            if (Interlocked.Exchange(ref mState, newState) == oldState)
                return true;

            return false;
        }*/

        public virtual void Initialize()
        {

            var old_state = mState;
            if (false == UpdateState(eServerState.Initialized))
            {
                Logger.Error($"Error in ServerModuleBase.Initialize() - Fail to Update State [{(eServerState)old_state}] -> [{(eServerState)mState}]");
                return;
            }
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
