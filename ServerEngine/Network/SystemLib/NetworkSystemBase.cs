using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

using ServerEngine.Network.Server;
using ServerEngine.Log;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// 소켓 통신(Listen, Accept, Connect)에 필요한 기능(공통기능)을 구현한 추상클래스
    /// </summary>
    public abstract class NetworkSystemBase : INetworkSystemBase
    {
        /// <summary>
        /// Accept 및 Connect callback 함수에서 session 추가시 사용되는 delegate
        /// </summary>
        protected Func<Session> mSessionCreater;

        /// <summary>
        /// 자신이 속한 서버모듈 
        /// </summary>
        public ServerModuleBase mServerModule { get; protected set; }

        /// <summary>
        /// 서버 Listen 관련정보
        /// </summary>
        public ListenInfo mListenInfo { get; private set; }

        /// <summary>
        /// 서버에서 공통적으로 사용되는 옵션이 포함된 객체
        /// </summary>
        public ServerConfig mServerInfo { get; private set; }

        /// <summary>
        /// 로거 클래스 
        /// </summary>
        public Logger logger { get; private set; }


        /// <summary>
        /// Accept 및 connect 상태
        /// </summary>
        protected int mSystemState = NetworkSystemState.NotInitialized;

        /// <summary>
        /// Thread 동기화에 사용될 Lock 객체
        /// </summary>

        protected object mLockObject = new object();

        /// <summary>
        /// IPEndpoint (ip, port 등..) 반환
        /// </summary>
        public IPEndPoint GetIPEndPoint => mListenInfo.ipEndpoint;

        /// <summary>
        /// Start 관련 작업에 사용되는 메서드
        /// </summary>
        /// <returns></returns>
        public abstract bool Start();

        /// <summary>
        /// Start 작업관련 한 번만 진행되어야하는 메서드
        /// </summary>
        /// <returns></returns>
        public abstract bool StartOnce();

        /// <summary>
        /// Listener 및 Connector 공용 메서드
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Stop 진행시 호출되는 event 핸들러
        /// </summary>
        public event EventHandler StopCallback;


        /// <summary>
        /// 클래스 멤버필드관련 초기화 메서드 
        /// * 호출하는 곳에서 파라미터에 대한 익셉션을 던지고 있기 때문에 이곳에서 별도로 처리하지 않는다
        /// </summary>
        public virtual void Initialize(ServerModuleBase module, IListenInfo listenInfo, ServerConfig serverInfo, Logger logger, Func<Session> creater)
        {
            if (listenInfo == null)
                throw new ArgumentNullException(nameof(listenInfo));

            if (serverInfo == null)
                throw new ArgumentNullException(nameof(serverInfo));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (creater == null)
                throw new ArgumentNullException(nameof(creater));

            mListenInfo = new ListenInfo(listenInfo.ip, listenInfo.port, listenInfo.backlog, listenInfo.serverName, listenInfo.nodelay);

            mServerInfo = serverInfo;
            this.logger = logger;
            mSessionCreater = creater;
            mServerModule = module;
        }

        protected virtual bool CheckCanStop()
        {
            var curState = mSystemState;

            if (curState == NetworkSystemState.Stopping)
                return false;

            if (curState == NetworkSystemState.StopCompleted)
                return false;

            return true;
        }

        public virtual bool ChangeState(int oldState, int newState)
        {
            var curState = mSystemState;
            if (curState == newState)
                return true;

            if (Interlocked.Exchange(ref mSystemState, newState) == oldState)
                return true;

            return false;
        }

        protected void OnStopCallback()
        {
            StopCallback?.Invoke(null, EventArgs.Empty);
        }
    }
}
