using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

using ServerEngine.Log;
using ServerEngine.Common;
using ServerEngine.Config;
using static ServerEngine.Network.SystemLib.SocketBase;

namespace ServerEngine.Network.SystemLib
{
    public abstract class SocketBase
    {
        // [0 0 0 0] [0 0 0 0] ([close][send/recv])
        // Socket 상태가 동시에 2개이상일 수 있어 [Flags] 애트리뷰트 사용
        [Flags]
        public enum eSocketState : int
        {
            None = 0,
            Sending = 1,
            SendComplete = 2,
            Recving = 4,
            RecvComplete = 8,
            Closing = 16,
            CloseComplete = 32
        }

        public enum eConnectState : int
        {
            NotConnected = 0,
            Connected = 1
        }

        protected Socket? mRawSocket;
        private object mLockObject = new object();
        private int mState = (int)eSocketState.None;
        private volatile int mConnected = 0;

        #region property
        public Log.ILogger Logger { get; protected set; }
        public Socket? GetSocket => mRawSocket;
        public int GetState => Volatile.Read(ref mState);
        #endregion

        protected SocketBase(Socket socket, Log.ILogger logger)
        {
            mRawSocket = socket;
            this.Logger = logger;   
        }

        public abstract void SetSocketOption(IConfigNetwork config_network);

        #region method
        public bool IsNullSocket()
        {
            return null == Interlocked.CompareExchange(ref mRawSocket, null, null);
        }

        public bool SetConnect(eConnectState flag)
        {
            if (eConnectState.NotConnected != flag && eConnectState.Connected != flag)
                throw new ArgumentException("Invalid flag value", nameof(flag));

            Interlocked.Exchange(ref mConnected, (int)flag);
            
            return true;
        }

        public bool IsConnected()
        {
            return (int)eConnectState.Connected == Interlocked.CompareExchange(ref mConnected, 
                                                                              (int)eConnectState.Connected, 
                                                                              (int)eConnectState.Connected);
        }

        public bool IsClosed()
        {
            // 원자적 Read 및 이후 스택변수인 state 값을 사용하여 비트연산 진행 (thread-safe)
            // mState 변수에 대한 쓰기작업은 Interlocked 메서드를 통해서만 진행되므로 쓰기작업이 thread-safe함이 보장됨
            var state = Volatile.Read(ref mState);

            var check_close = state & (int)eSocketState.Closing;
            if ((int)eSocketState.Closing == check_close)
                return true;

            var check_close_complete = state & (int)eSocketState.CloseComplete;
            if ((int)eSocketState.CloseComplete == check_close_complete)
                return true;

            return false;
        }

        public void UpdateState(eSocketState state)
        {
            Interlocked.Exchange(ref mState, (int)state);
        }

        public void RemoveState(eSocketState state)
        {
            // 원자적 Read 및 이후 스택변수인 state 값을 사용하여 비트연산 진행 (thread-safe)
            var old_state = Volatile.Read(ref mState);
            var new_state = old_state & ~(int)state;

            Interlocked.Exchange(ref mState, new_state);
        }

        public bool CheckState(eSocketState state)
        {
            // 원자적 Read 및 이후 스택변수인 state 값을 사용하여 비트연산 진행 (thread-safe)
            var old_state = Volatile.Read(ref mState);
            var confirm_state = (int)state;

            return confirm_state == (old_state & confirm_state);
        }

        public bool CheckNotSendRecv()
        {
            if (false == CheckState(eSocketState.Sending) &&
                false == CheckState(eSocketState.Recving))
                return true;

            return false;
        }

        public virtual void DisconnectSocket(SocketShutdown shutdown_option = SocketShutdown.Both)
        {
            if (true == IsNullSocket() || true == IsClosed())
                return;

            UpdateState(eSocketState.Closing);

            try
            {
                lock (mLockObject)
                {
                    if (null == mRawSocket)
                        return;

                    if (mRawSocket.Connected)
                        mRawSocket.Shutdown(shutdown_option);

                    mRawSocket.Dispose();

                    UpdateState(eSocketState.CloseComplete);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void InternalClose(ServerSession.Session session, eCloseReason reason, Action<ServerSession.Session, eCloseReason> close_event)
        {
            try
            {
                if (null == close_event)
                    throw new ArgumentNullException(nameof(close_event));

                if (false == CheckNotSendRecv())
                    return;

                close_event?.Invoke(session, reason);

                DisconnectSocket();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public virtual void Dispose()
        {
            mRawSocket?.Dispose();

            Interlocked.Exchange(ref mState, (int)eSocketState.None);
        }
        #endregion

        #region "Socket State Change"
        //protected int mSocketState = ServerState.NotInitialized;
        //public int GetSocketState => mSocketState;
        //public bool IsSocketNull => mRawSocket == null;
        //public bool IsInClosingOrClosed => mSocketState >= SocketState.Closing;

        // Socket 상태의 경우 send/recv가 동시에 일어날 수 있기 때문에 state에 상태가 중복저장될 수 있다. 따라서 비트연산진행        
        // 해당 메서드는 상태가 중복해서 저장될 수 있는 경우 호출 (ex: recv)
        /*public bool AddState(int state, bool checkClose)
        {
            while(true)
            {
                var oldState = mSocketState;

                if (checkClose)
                {
                    if (oldState >= SocketState.Closing)
                        return false;
                }

                var newState = mSocketState | state;

                if (oldState == Interlocked.CompareExchange(ref mSocketState, newState, oldState))
                    return true;
            }
        }
        */

        // 해당 메서드는 상태가 중복해서 저장되지 않는 경우 호출 (ex: send, close)
        /*public bool TryAddState(int state)
        {
            while(true)
            {
                var oldState = mSocketState;
                var newState = mSocketState | state;

                // 이미 바꾸려는 상태일 경우 false 리턴
                if (oldState == newState)
                    return false;

                if (oldState == Interlocked.CompareExchange(ref mSocketState, newState, oldState))
                    return true;
            }
        }
        */

        // 해당 메서드는 기존 상태를 제거하고 다른 상태로 변경할 때 호출 (ex: close) 
        /*public bool TryRemoveAddState(int remove, int add)
        {
            while(true)
            {
                var oldState = mSocketState;
                var removeState = mSocketState & ~remove;

                if (oldState == Interlocked.CompareExchange(ref mSocketState, removeState, oldState))
                {
                    var addState = removeState | add;
                    if (removeState == Interlocked.CompareExchange(ref mSocketState, addState, removeState))
                        return true;
                    else
                    {
                        Interlocked.Exchange(ref mSocketState, oldState);
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
        */

        /*public bool RemoveState(int state)
        {
            while(true)
            {
                var oldState = mSocketState;
                var newState = mSocketState & ~state;

                if (oldState == Interlocked.CompareExchange(ref mSocketState, newState, oldState))
                    return true;
            }
        }*/

        /*public bool CheckState(int state)
        {
            return (mSocketState & state) == state;
        }
        */

        /*public bool CheckNotInReceivingAndSending()
        {
            var state = SocketState.Receiving | SocketState.Sending;
            return mSocketState != state;
        }*/

        #endregion
        /*public void InternalClose(ServerSession.Session session, eCloseReason reason, Action<ServerSession.Session, eCloseReason> closeEvent)
        {
            try
            {
                if (CheckNotInReceivingAndSending())
                    closeEvent?.Invoke(session, reason);

                DisconnectSocket();
            }
            catch (ArgumentNullException argNullEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), argNullEx);
                return;
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
                return;
            }
        }
        */

        // Thread Safe 하게해야되나?
        /* public virtual void DisconnectSocket()
        {
            if (IsClosed || IsSocketNull)
                return;

            if (!TryRemoveAddState(SocketState.Closing, SocketState.Closed))
            {
                logger.Error(this.ClassName(), this.MethodName(), "Fail to Change Socket state [Closed]");
                return;
            }
            else
            {
                if (mRawSocket.Connected)
                    mRawSocket.Shutdown(SocketShutdown.Both);

                mRawSocket.Close();

                var oldRawSocket = mRawSocket;
                if (oldRawSocket != Interlocked.Exchange(ref mRawSocket, null))
                {
                    logger.Error(this.ClassName(), this.MethodName(), "Fail to Change Socket null");
                    return;
                }
            }
        }
        */
    }
}
