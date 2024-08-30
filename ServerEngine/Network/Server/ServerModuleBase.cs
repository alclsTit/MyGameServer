using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.ObjectPool;

using ServerEngine.Common;
using ServerEngine.Log;
using ServerEngine.Network.SystemLib;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;

namespace ServerEngine.Network.Server
{
    public abstract class ServerModuleBase : IServerModule
    {
        public int mState = ServerState.NotInitialized;

        public string Name { get; private set; }

        public Log.ILogger Logger { get; set; }

        #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
        /*
        public SocketAsyncEventArgsPool mRecvEventPool { get; private set; }
        public SocketAsyncEventArgsPool mSendEventPool { get; private set; }
        */
        #endregion

        #region "Microsoft.Extensions.ObjectPool"
        private DefaultObjectPoolProvider mPoolProvider = new DefaultObjectPoolProvider();
        private SocketEventArgsObjectPoolPolicy mPoolPolicy = new SocketEventArgsObjectPoolPolicy();
        public Microsoft.Extensions.ObjectPool.ObjectPool<SocketAsyncEventArgs> mRecvEventPoolFix { get; private set; }
        public Microsoft.Extensions.ObjectPool.ObjectPool<SocketAsyncEventArgs> mSendEventPoolFix { get; private set; }
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
        public IConfigListen mListenInfo { get; private set; }

        /// <summary>
        /// 모든 리슨정보
        /// </summary>
        public List<IConfigListen> mListenInfoList { get; private set; }

        public IPEndPoint ipEndPoint { get; private set; }

        /// <summary>
        /// 각각의 서버모듈이 작동하기 시작한 시작시간
        /// </summary>
        public DateTime mStartTime { get; protected set; }

        /// <summary>
        /// 이전 서버모듈이 작동을 시작한 시간
        /// </summary>
        public DateTime mLastActiveTime { get; protected set; }

        public int GetServerState => mState;

        protected ServerModuleBase(string name)
        {
            this.Name = name;
            mStartTime = DateTime.Now;
            mLastActiveTime = mStartTime;
        }

        public bool ChangeState(int oldState, int newState)
        {
            var curState = mState;
            if (curState == newState)
                return true;

            if (Interlocked.Exchange(ref mState, newState) == oldState)
                return true;

            return false;
        }

        public virtual void Initialize(List<IConfigListen> listenInfoList, IConfigListen config_listen, ServerConfig serverInfo, INetworkSystemBase networkSystem, Log.ILogger logger, Func<Session> creater)
        {
            if (listenInfoList == null)
                throw new ArgumentNullException(nameof(listenInfoList));

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
            this.Logger = logger;
            
            mNetworkSystem = networkSystem;

            #region "2022.05.04 기존 커스텀 ObjectPool -> Microsoft.Extensions.ObjectPool로 변경에 따른 코드 주석처리"
            /*
            mRecvEventPool = new SocketAsyncEventArgsPool(mServerInfo.maxConnectNumber);
            mSendEventPool = new SocketAsyncEventArgsPool(mServerInfo.maxConnectNumber);
            */
            #endregion

            // Microsoft.Extensions.ObjectPool 사용
            // ObjectPool에서 관리하는 최대 Chunk 수 세팅
            mPoolProvider.MaximumRetained = mServerInfo.maxConnectNumber;

            // ObjectPool 객체생성 정책에 맞춰서 Recv/Send SocketAsyncEventArgs Pool 생성
            mRecvEventPoolFix = mPoolProvider.Create(mPoolPolicy);
            mSendEventPoolFix = mPoolProvider.Create(mPoolPolicy);

            var oldState = ServerState.NotInitialized;
            if (!ChangeState(oldState, ServerState.Initialized))
            {
                logger.Error($"Error in ServerModuleBase.Initialize() - State is [{oldState}]. It can be [Initialized] when state is [NotInitialized]");
                return;
            }

            // 마지막에 초기화 진행
            mNetworkSystem.Initialize(this, config_listen, serverInfo, logger, creater);
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
            session.SetRecvEventByPool(mRecvEventPoolFix.Get());
            session.SetSendEventByPool(mSendEventPoolFix.Get());

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
            mRecvEventPoolFix.Return(session.mRecvEvent);
            mSendEventPoolFix.Return(session.mSendEvent);

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
