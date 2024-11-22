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

        public enum eNetworkSystemType
        {
            None = 0,
            Accept = 1,
            Connect = 2
        }

        /// <summary>
        /// 자신이 속한 서버모듈 
        ///     - 순환참조되므로 약한 참조를 사용
        /// </summary>
        public WeakReference<ServerModule> ServerModule { get; protected set; }

        /// <summary>
        /// 로거 클래스 
        /// </summary>
        public Log.ILogger Logger { get; protected set; }

        /// <summary>
        /// Accept 및 connect 상태
        /// </summary>
        protected volatile int mState;

        /// <summary>
        /// Connector, Acceptor 구분 필드
        /// </summary>
        public eNetworkSystemType mType;

        protected NetworkSystemBase(Log.ILogger logger, ServerModule module)
        {
            this.Logger = logger;
            ServerModule = new WeakReference<ServerModule>(module);
        }

        public abstract bool Initialize();
        public abstract bool Initialize(IConfigListen config_listen, IConfigEtc config_etc);
        public abstract bool Start();
        public abstract bool Start(string address, ushort port, bool client_connect = true);
        public abstract void Stop();

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
            return (int)state == mState;
        }

        public void UpdateState(eNetworkSystemState state)
        {
            Interlocked.Exchange(ref mState, (int)state);
        }

        protected void OnStopCallback()
        {
            //StopCallback?.Invoke(null, EventArgs.Empty);
        }
        #endregion
    }
}
