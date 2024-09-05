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
using ServerEngine.Common;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// 소켓 통신(Listen, Accept, Connect)에 필요한 기능(공통기능)을 구현한 추상클래스
    /// </summary>
    public abstract class NetworkSystemBase
    {
        public enum eNetworkSystemState
        {
            None = 0,
            Initialized = 1,
            Running = 2,
            Stopping = 3,
            StopComplete = 4
        }

        /// <summary>
        /// Accept 및 Connect callback 함수에서 session 추가시 사용되는 delegate
        /// </summary>
        //protected Func<Session> mSessionCreater;

        /// <summary>
        /// 자신이 속한 서버모듈 
        /// </summary>
        public ServerModuleBase ServerModule { get; protected set; }

        /// <summary>
        /// 서버 Listen 관련정보
        /// </summary>
        //public ListenInfo mListenInfo { get; private set; }

        /// <summary>
        /// 서버에서 공통적으로 사용되는 옵션이 포함된 객체
        /// </summary>
        //public ServerConfig mServerInfo { get; private set; }

        /// <summary>
        /// 로거 클래스 
        /// </summary>
        public Log.ILogger Logger { get; protected set; }

        /// <summary>
        /// Accept 및 connect 상태
        /// </summary>
        protected volatile int mState = (int)eNetworkSystemState.None;

        /// <summary>
        /// Listener 및 Connector 공용 메서드
        /// </summary>
        public abstract void Stop();

        /// <summary>
        /// Stop 진행시 호출되는 event 핸들러
        /// </summary>
        //public event EventHandler StopCallback;

        /// <summary>
        /// Socket Connect 관련 상태 플래그 (1:연결 / 0:비연결)
        /// </summary>
        //public int mConnected = 0;

        //public bool IsConnected => mConnected == 1 ? true : false;

        protected NetworkSystemBase(Log.ILogger logger, ServerModuleBase server_module)
        {
            this.Logger = logger;
            ServerModule = server_module;
        }

        public abstract bool Initialize();
        public abstract bool Initialize(IConfigListen config_listen, IConfigEtc config_etc);
        public abstract bool Start();
        public abstract bool Start(string address, ushort port, bool client_connect = true);

        #region public_method
        public bool CheckStop()
        {
            var old_state = (int)mState;

            if (old_state == Interlocked.CompareExchange(ref mState, (int)eNetworkSystemState.Stopping, (int)eNetworkSystemState.Stopping))
                return true;

            if (old_state == Interlocked.CompareExchange(ref mState, (int)eNetworkSystemState.StopComplete, (int)eNetworkSystemState.StopComplete))
                return true;

            return false;
        }

        public virtual bool CheckState(eNetworkSystemState state)
        {
            var old_state = (int)mState;
            var check_state = (int)state;

            return old_state == Interlocked.CompareExchange(ref mState, check_state, check_state) ? true : false;
        }

        public bool UpdateState(eNetworkSystemState state)
        {
            var old_state = (int)state;
            if (old_state == mState)
                return true;

            return old_state == Interlocked.Exchange(ref mState, (int)state);
        }

        protected void OnStopCallback()
        {
            StopCallback?.Invoke(null, EventArgs.Empty);
        }
        #endregion

        /// <summary>
        /// 클래스 멤버필드관련 초기화 메서드 
        /// * 호출하는 곳에서 파라미터에 대한 익셉션을 던지고 있기 때문에 이곳에서 별도로 처리하지 않는다
        /// </summary>
        /*public virtual void Initialize(ServerModuleBase module, ServerConfig serverInfo, Log.Logger logger, Func<Session> creater)
        {
            if (listenInfo == null)
                throw new ArgumentNullException(nameof(listenInfo));

            if (serverInfo == null)
               throw new ArgumentNullException(nameof(serverInfo));

            if (logger == null)
                throw new ArgumentNullException(nameof(logger));

            if (creater == null)
                throw new ArgumentNullException(nameof(creater));

            mServerInfo = serverInfo;
            this.Logger = logger;
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
        
        /// <summary>
        /// Socket Connect 상태를 원자적으로 변경
        /// *bool 타입의 경우 Interlocked에서 지원해주지 않기 때문에 true = 1 / false = 0 으로 치환하여 작업 
        /// </summary>
        /// <param name="flag">변경할 socket connect 값</param>
        public bool ChangeConnectState(bool flag)
        {
            var curState = mConnected;
            var flagToInt = flag == true ? 1 : 0;
            return curState == Interlocked.Exchange(ref mConnected, flagToInt) ? true : false;
        }
        */
    }
}
