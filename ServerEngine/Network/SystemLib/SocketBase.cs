using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

using ServerEngine.Log;
using ServerEngine.Common;

namespace ServerEngine.Network.SystemLib
{
    public abstract class SocketBase
    {
        protected Socket mRawSocket;

        protected int mSocketState = ServerState.NotInitialized;

        public Logger logger { get; protected set; }

        public int GetSocketState => mSocketState;

        public bool IsSocketNull => mRawSocket == null;

        public bool IsClosed => mSocketState == SocketState.Closed;

        public bool IsInClosingOrClosed => mSocketState >= SocketState.Closing;

        public Socket GetSocket()
        {
            return mRawSocket;
        }

        public bool CheckCanClose()
        {
            var curState = mSocketState;

            if (curState == SocketState.Closing)
                return false;

            if (curState == SocketState.Closed)
                return false;

            return true;
        }

        public bool ChangeState(int oldState, int newState)
        {
            if (Interlocked.Exchange(ref mSocketState, newState) == oldState)
                return true;

            return false;
        }

        #region "Socket State Change"
        // Socket 상태의 경우 send/recv가 동시에 일어날 수 있기 때문에 state에 상태가 중복저장될 수 있다. 따라서 비트연산진행        
        // 해당 메서드는 상태가 중복해서 저장될 수 있는 경우 호출 (ex: recv)
        public bool AddState(int state, bool checkClose)
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
             
        // 해당 메서드는 상태가 중복해서 저장되지 않는 경우 호출 (ex: send, close)
        public bool TryAddState(int state)
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

        // 해당 메서드는 기존 상태를 제거하고 다른 상태로 변경할 때 호출 (ex: close) 
        public bool TryRemoveAddState(int remove, int add)
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

        public bool RemoveState(int state)
        {
            while(true)
            {
                var oldState = mSocketState;
                var newState = mSocketState & ~state;

                if (oldState == Interlocked.CompareExchange(ref mSocketState, newState, oldState))
                    return true;
            }
        }

        public bool CheckState(int state)
        {
            return (mSocketState & state) == state;
        }

        public bool CheckNotInReceivingAndSending()
        {
            var state = SocketState.Receiving | SocketState.Sending;
            return mSocketState != state;
        }

        #endregion
        public void InternalClose(ServerSession.Session session, eCloseReason reason, Action<ServerSession.Session, eCloseReason> closeEvent)
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

        // Thread Safe 하게해야되나?
        public virtual void DisconnectSocket()
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
    }
}
