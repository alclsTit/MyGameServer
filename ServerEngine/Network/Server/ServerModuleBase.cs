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
using ServerEngine.Config;
using ServerEngine.Network.ServerSession;

namespace ServerEngine.Network.Server
{
    public abstract class ServerModuleBase : IServerModule
    {
        public int mState = ServerState.NotInitialized;

        public string name { get; private set; }

        public Logger logger { get; private set; }

        public SocketAsyncEventArgsPool mRecvEventPool { get; private set; }

        public SocketAsyncEventArgsPool mSendEventPool { get; private set; }

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
        public IListenInfo mListenInfo { get; private set; }

        /// <summary>
        /// 모든 리슨정보
        /// </summary>
        public List<IListenInfo> mListenInfoList { get; private set; }

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

        protected ServerModuleBase()
        {
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

        public virtual void Initialize(List<IListenInfo> listenInfoList, IListenInfo listenInfo, ServerConfig serverInfo, INetworkSystemBase networkSystem, Logger logger, Func<Session> creater)
        {
            if (listenInfoList == null)
                throw new ArgumentNullException(nameof(listenInfoList));

            if (listenInfo == null)
                throw new ArgumentNullException(nameof(listenInfo));

            if (serverInfo == null)
                throw new ArgumentNullException(nameof(serverInfo));

            if (networkSystem == null)
                throw new ArgumentNullException(nameof(networkSystem));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (creater == null)
                throw new ArgumentNullException(nameof(creater));

            mListenInfoList = listenInfoList;
            mListenInfo = listenInfo;
            name = listenInfo.serverName;
            ipEndPoint = ServerHostFinder.GetServerIPAddress(listenInfo.port);

            mServerInfo = serverInfo;
            this.logger = logger;
            
            mNetworkSystem = networkSystem;

            mRecvEventPool = new SocketAsyncEventArgsPool(mServerInfo.maxConnectNumber);
            mSendEventPool = new SocketAsyncEventArgsPool(mServerInfo.maxConnectNumber);

            var oldState = ServerState.NotInitialized;
            if (!ChangeState(oldState, ServerState.Initialized))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"State is [{oldState}]. It can be [Initialized] when state is [NotInitialized]");
                return;
            }

            // 마지막에 초기화 진행
            mNetworkSystem.Initialize(this, listenInfo, serverInfo, logger, creater);
            mNetworkSystem.StopCallback += OnNetworkSystemStopped;
        }

        public void InitializeSessionManager(ServerSession.ISessionManager sessionManager)
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

            session.Closed += OnSessionClosed;
            session.SetRecvEventByPool(mRecvEventPool.Pop());
            session.SetSendEventByPool(mSendEventPool.Pop());

            return session;
        }

        public void OnSessionClosed(Session session, eCloseReason reason)
        {
            if (session == null)
            {
                logger.Error(this.ClassName(), this.MethodName(), "Session Object is null!!!");
                return;
            }

            session.ClearAllSocketAsyncEvent();

            mRecvEventPool.Push(session.mRecvEvent);
            mSendEventPool.Push(session.mSendEvent);

            mSessionManager.Close(session.mSessionID);
        }

        private void OnNetworkSystemStopped(object sender, EventArgs e)
        {
            if (logger.IsDebugEnabled)
                logger.Debug($"[{mListenInfo.serverName}][{ipEndPoint.Address.ToString()}:{mListenInfo.port}] was stopped");
        }

        public string GetConnectedServerName(EndPoint endPoint)
        {
            var ipEndPoint = (IPEndPoint)endPoint;
            var listenInfo = mListenInfoList.Find((item) => { return item.port == ipEndPoint.Port; });
            return listenInfo != null ? listenInfo.serverName : default(string);    
        }

    }
}
