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
        private volatile int mState = (int)eSocketState.None;
        private volatile int mConnected = 0;

        // protected int mSocketState = ServerState.NotInitialized;

        //public int GetSocketState => mSocketState;

        //public bool IsSocketNull => mRawSocket == null;

        #region property
        public Log.ILogger Logger { get; protected set; }
        public Socket? GetSocket => mRawSocket;
        public int GetState => mState;
        #endregion


        //public bool IsInClosingOrClosed => mSocketState >= SocketState.Closing;

        protected SocketBase(Socket socket, Log.ILogger logger)
        {
            mRawSocket = socket;
            this.Logger = logger;   
        }

        public abstract void SetSocketOption(IConfigNetwork config_network);

        #region method
        public bool IsNullSocket()
        {
            var origin = Interlocked.CompareExchange(ref mRawSocket, null, null);
            return null == origin ? true : false;
        }

        public bool SetConnect(eConnectState flag)
        {
            if (eConnectState.NotConnected != flag || eConnectState.Connected != flag)
                return false;

            int new_state = (int)flag;
            var old_connected = mConnected;

            return old_connected == Interlocked.Exchange(ref mConnected, new_state) ? true : false;
        }

        public bool IsConnected()
        {
            var old_connected = mConnected;
            return old_connected == Interlocked.CompareExchange(ref mConnected, 1, 1) ? true : false;
        }

        public bool IsClosed()
        {
            var state = mState;

            var check_close = state & (int)eSocketState.Closing;
            if ((int)eSocketState.Closing == check_close)
                return true;

            var check_close_complete = state & (int)eSocketState.CloseComplete;
            if ((int)eSocketState.CloseComplete == check_close_complete)
                return true;

            return false;
        }

        // 삭제예정
        /*public bool CheckCanClose()
         {
             var curState = mSocketState;

             if (curState == SocketState.Closing)
                 return false;

             if (curState == SocketState.Closed)
                 return false;

             return true;
         }
         */

        // 삭제 예정
        /*
        public bool ChangeState(int oldState, int newState)
        {
            if (Interlocked.Exchange(ref mSocketState, newState) == oldState)
                return true;

            return false;
        }
        */
        public bool UpdateState(eSocketState state)
        {
            var old_state = mState;
            var new_state = old_state | (int)state;

            return old_state == Interlocked.Exchange(ref mState, new_state) ? true : false;
        }

        public bool RemoveState(eSocketState state)
        {
            var old_state = mState;
            var new_state = old_state & ~(int)state;

            return old_state == Interlocked.Exchange(ref mState, new_state) ? true : false;
        }

        public bool CheckState(eSocketState state)
        {
            var old_state = mState;
            var check_state = (int)state;

            return check_state == (old_state & check_state);
        }

        public bool CheckNotSendRecv()
        {
            var check_recv = CheckState(eSocketState.Recving);
            var check_send = CheckState(eSocketState.Sending);
            
            return check_recv && check_send;
        }

        public virtual void DisconnectSocket(SocketShutdown shutdown_option = SocketShutdown.Both)
        {
            if (true == IsNullSocket() || true == IsClosed())
                return;

            eSocketState new_state = eSocketState.Closing;
            if (false == UpdateState(new_state))
            {
                Logger.Error($"Error in SocketBase.DisconnectSocket() - Fail to update state [{new_state}]");
                return;
            }
            else
            {
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
