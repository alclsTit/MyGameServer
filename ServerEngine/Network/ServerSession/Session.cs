using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using ServerEngine.Config;
using ServerEngine.Common;
using ServerEngine.Network.SystemLib;
using ServerEngine.Log;
using ServerEngine.Network.Message;


namespace ServerEngine.Network.ServerSession
{
    public abstract class Session
    {
        public string mSessionID { get; private set; }

        public bool mIsClient { get; private set; }

        public TCPSocket mSocketWrapper { get; private set; }

        public SocketAsyncEventArgs mRecvEvent { get; private set; }

        public SocketAsyncEventArgs mSendEvent { get; private set; }

        /// <summary>
        /// 세션에 연결된 클라이언트 IP, PORT 정보 및 서버의 IP, PORT 정보 
        /// </summary>
        public IPEndPoint mRemoteIPEndPoint { get; private set; }
        public IPEndPoint mLocalIPEndPoint { get; private set; }

        /// <summary>
        /// Session 객체에서 사용할 서버 옵션
        /// </summary>
        public ServerConfig mServerInfo { get; private set; }


        /// <summary>
        /// 모든 리슨정보
        /// </summary>
        public List<IListenInfo> mListenInfoList { get; private set; }

        /// <summary>
        /// Session 객체에서 사용할 Logger 클래스
        /// </summary>
        public Logger logger { get; private set; }

        #region "Send관련 필드"
        /// <summary>
        /// Send 시 메시지를 모아서 보내기위해 메시지관리하는 큐
        /// * 세션을 생성했다는 것을 recv/send를 진행한다는 뜻
        /// </summary>
        public Queue<ArraySegment<byte>> mSendingQueue { get; private set; } = new Queue<ArraySegment<byte>>();

        /// <summary>
        /// Send 될 패킷을 담은 리스트
        /// </summary>
        public List<ArraySegment<byte>> mSendingList { get; private set; } = new List<ArraySegment<byte>>();

        /// <summary>
        /// Send 가능한지 확인
        /// </summary>
        public bool IsAbleToSend => mSendingList.Count <= 0;
        #endregion

        /// <summary>
        /// Recv 처리관련 클래스
        /// </summary>
        public MessageProcessor mMessageProcessor;

        protected object mLockObject { get; private set; } = new object();

        public Action<Session, eCloseReason> Closed { get; set; }

        protected abstract bool CheckRecvPacketValidate(ArraySegment<byte> buffer);

        protected abstract bool CheckSendPacketValidate(ArraySegment<byte> buffer, ushort size);

        public abstract void OnConnected(EndPoint endPoint, Session session = null);

        public abstract int OnReceive(ArraySegment<byte> buffer);

        public abstract void OnSend(int numOfBytes);
        
        public virtual void Initialize(string sessionID, Socket clientSocket, ServerConfig config, Logger logger, List<IListenInfo> listenInfoList, bool isClient)
        {
            mSessionID = sessionID;
            mIsClient = isClient;

            this.logger = logger;

            mServerInfo = config;

            // 통신용 소켓 세팅
            mSocketWrapper = new TCPSocket();
            mSocketWrapper.Initialize(clientSocket, config, logger);
            
            // 소켓 통신이 되는 클라이언트, 서버의 IP, PORT 정보
            mRemoteIPEndPoint = (IPEndPoint)clientSocket.RemoteEndPoint;   
            mLocalIPEndPoint = (IPEndPoint)clientSocket.LocalEndPoint;

            // Recv 처리관련 클래스 인스턴스 생성
            mMessageProcessor = new MessageProcessor(config.recvBufferSize);

            mListenInfoList = listenInfoList;
        }

        public void SetRecvEventByPool(SocketAsyncEventArgs poolVal)
        {
            mRecvEvent = poolVal;
            mRecvEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
        }

        public void SetSendEventByPool(SocketAsyncEventArgs poolVal)
        {
            mSendEvent = poolVal;
            mSendEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);
        }

        #region "Recv Logic"
        /// <summary>
        /// 세션이 생성되었으므로 처음으로 Recv 시작
        /// </summary>
        public virtual void StartReceive()
        {
            ReceiveAsync();
        }

        private bool SocketReceiveAsync()
        {
            return mSocketWrapper.GetSocket().ReceiveAsync(mRecvEvent);
        }

        private bool CheckStartReceive()
        {
            // Recv 도중에 소켓의 상태가 Closing이 된 경우 recv를 멈추고 session close 루틴진행
            if (mSocketWrapper.AddState(SocketState.Receiving, true) && mRecvEvent != null)
                return true;

            CheckValidateClose(false, false, eCloseReason.Unknown);
            return false;
        }

        private void ReceiveAsync()
        {          
            if (!CheckStartReceive())
                return;

            mMessageProcessor.Clear();
            var segment = mMessageProcessor.GetWriteMessage;
            mRecvEvent.SetBuffer(segment.Array, segment.Offset, segment.Count);

            var lPending = SocketReceiveAsync();
            if (!lPending)
                OnRecvCompleted(null, mRecvEvent);
        }

        private void OnRecvCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (!AsyncCallbackChecker.CheckCallbackHandler(e.SocketError, e.BytesTransferred))
            {
                OnReceiveEnd(e.SocketError == SocketError.Success ? eCloseReason.ClientClose : eCloseReason.SocketError);
                logger.Error(this.ClassName(), this.MethodName(), $"[SocketError = {e.SocketError}, BytesTransferred = {e.BytesTransferred}]");
                return;
            }

            mSocketWrapper.RemoveState(SocketState.Receiving);

            try
            {
                if (!mMessageProcessor.OnWriteMessage(e.BytesTransferred))
                {
                    OnReceiveEnd();
                    return;
                }

                var processLength = OnReceive(mMessageProcessor.GetReadMessage);
                if (processLength < 0 || processLength > mMessageProcessor.GetDataSize)
                {
                    OnReceiveEnd();
                    return;
                }

                if (!mMessageProcessor.OnReadMessage(processLength))
                {
                    OnReceiveEnd();
                    return;
                }

                ReceiveAsync();
            }
            catch (IndexOutOfRangeException outOfRangeEx)
            {
                OnReceiveEnd(eCloseReason.SeverException);
                logger.Error(this.ClassName(), this.MethodName(), outOfRangeEx);
                return;
            }
            catch (Exception ex)
            {
                OnReceiveEnd(eCloseReason.SeverException);
                logger.Error(this.ClassName(), this.MethodName(), ex);
                return;
            }

        }
        #endregion

        #region "Send Logic"
        /// <summary>
        /// 패킷 send. 여러 곳에서 접근가능
        /// </summary>
        public void StartSend(ArraySegment<byte> message)
        {
            if (message == default(ArraySegment<byte>))
                return;

            if (!mSocketWrapper.TryAddState(SocketState.Sending))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Socket state is [{mSocketWrapper.GetSocketState}]. Fail to change state [Sending]");
                return;
            }

            lock (mLockObject)
            {
                mSendingQueue.Enqueue(message);
                if (IsAbleToSend)
                    SendAsync();
            }
        }

        private bool SocketSendAsync()
        {
            return mSocketWrapper.GetSocket().SendAsync(mSendEvent);
        }

        /// <summary>
        /// 패킷 send 준비단계. 큐잉 처리 
        /// </summary>
        /// <param name="message"></param>
        public void SendAsync()
        {
            if (mSocketWrapper.IsInClosingOrClosed || mSocketWrapper.IsSocketNull || mSendEvent == null)
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Socket [SendError]. Socket state is {mSocketWrapper.GetSocketState}");
                OnSendEnd();
                return;
            }

            var numOfSendQueue = mSendingQueue.Count;
            while (numOfSendQueue > 0)
            {
                mSendingList.Add(mSendingQueue.Dequeue());
                --numOfSendQueue;
            }
            mSendEvent.BufferList = mSendingList;

            var lPending = SocketSendAsync();
            if (!lPending)
                OnSendCompleted(null, mSendEvent);
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (!AsyncCallbackChecker.CheckCallbackHandler(e.SocketError, e.BytesTransferred))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"[SocketError = {e.SocketError}, BytesTransferred = {e.BytesTransferred}");
                OnSendEnd(false, eCloseReason.SocketError);
                return;
            }

            try
            {
                lock(mLockObject)
                {
                    // Send 버퍼리스트에 있는 대상을 모두 보낸 상태
                    mSendEvent.BufferList = null;
                    mSendingList.Clear();

                    OnSend(mSendEvent.BytesTransferred);

                    if (mSendingQueue.Count > 0)
                    {
                        SendAsync();
                    }
                    else
                    {
                        OnSendEnd();

                        if (mSendingQueue.Count > 0)
                            SendAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
                OnSendEnd(false, eCloseReason.SeverException);
                return;
            }
        }

        /// <summary>
        /// Todo: Threadsafe 작업필요
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="message"></param>
        public void Relay<THandler>(THandler message) where THandler : Packet
        {
            if (mSocketWrapper.IsSocketNull)
                return;

            if (mSocketWrapper.IsInClosingOrClosed)
                return;

            var packetBuffer = PacketParser.Instance.MessageToBuffer(message);
            if (packetBuffer == default(ArraySegment<byte>))
                return;

            if (!CheckSendPacketValidate(packetBuffer, message.size))
                return;

            StartSend(packetBuffer);
        }


        #endregion

        #region "Close Logic"   
        protected void OnSendEnd()
        {
            OnSendEnd(false, eCloseReason.Unknown);
        }

        protected void OnSendEnd(bool forceClose, eCloseReason reason = eCloseReason.Unknown)
        {
            mSocketWrapper.RemoveState(SocketState.Sending);

            CheckValidateClose(forceClose, true, reason);
        }

        protected void OnReceiveEnd(eCloseReason reason = eCloseReason.Unknown)
        {
            mSocketWrapper.RemoveState(SocketState.Receiving);
            // 받는 쪽에서 잘못된 경우 강제로 소켓 close 처리를한다
            CheckValidateClose(true, false, reason);
        }

        private void CheckValidateClose(bool forceClose, bool IsSend, eCloseReason reason)
        {
            // Close 관련 처리시 해당 클래스에 아예 접근하지 못하도록 하기위해서
            lock(this)
            {
                if (mSocketWrapper.IsClosed || mSocketWrapper.IsSocketNull)
                    return;

                if (mSocketWrapper.CheckState(SocketState.Closing))
                {
                    if (IsSend)
                    {
                        if (forceClose || mSendingQueue != null)
                        {
                            mSendingList.Clear();
                            mSendingQueue.Clear();
                        }
                    }

                    try
                    {
                        if (mSocketWrapper.CheckNotInReceivingAndSending())
                            Closed?.Invoke(this, reason);
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
                else if (forceClose)
                {
                    Close(reason);
                }
            }
        }

        private void Close(eCloseReason reason)
        {
            if (!mSocketWrapper.TryAddState(SocketState.Closing))
                return;

            if (mSocketWrapper.IsSocketNull)
                return;

            // 현재 패킷 sending 중일 경우 관련 처리를 한 뒤 close 
            // * 실제 sending 진행 중일 때 이곳을 타면 진입하는 것 확인
            if (mSocketWrapper.CheckState(SocketState.Sending))
            {
                logger.Warn("Check before closing when state is sending!!!");
                return;
            }

            mSocketWrapper.InternalClose(this, reason, Closed);
        }

        public void ClearAllSocketAsyncEvent()
        {
            if (mRecvEvent == null && mSendEvent == null)
                return;
            else
            {
                if (mRecvEvent == null)
                    ClearSocketAsyncEvent(mRecvEvent);
                else if (mSendEvent == null)
                    ClearSocketAsyncEvent(mSendEvent);
                else
                {
                    ClearSocketAsyncEvent(mRecvEvent);
                    ClearSocketAsyncEvent(mSendEvent);
                }
            }
        }

        private void ClearSocketAsyncEvent(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            e.RemoteEndPoint = null;
            e.UserToken = null;

            e.SetBuffer(null, 0, 0);
            e.BufferList?.Clear();
        }
        #endregion
    }
}
