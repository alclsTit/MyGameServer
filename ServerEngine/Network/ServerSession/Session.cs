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

        /// <summary>
        /// 소켓 IO 작업에 사용되는 소켓 클래스
        /// </summary>
        public TcpSocket mSocketWrapper { get; private set; }

        /// <summary>
        /// Send용 SocketAsyncEventArgs  
        /// </summary>
        public SocketAsyncEventArgs mRecvEvent { get; private set; }

        /// <summary>
        /// Recv용 SocketAsyncEventArgs  
        /// </summary>
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
        /// Send 될 패킷을 담은 리스트, SocketAsyncEventArgs.BufferList에 사용
        /// </summary>
        public List<ArraySegment<byte>> mSendingList { get; private set; } = new List<ArraySegment<byte>>();

        /// <summary>
        /// Send 가능한지 확인
        /// </summary>
        public bool IsAbleToSend => mSendingList.Count <= 0;
        #endregion

        /// <summary>
        /// Recv된 패킷처리 담당 클래스
        /// </summary>
        public MessageProcessor mMessageProcessor;

        private readonly object mLockObject = new object();

        public Action<Session, eCloseReason> Closed { get; set; }

        protected abstract bool CheckRecvPacketValidate(ArraySegment<byte> buffer);

        protected abstract bool CheckSendPacketValidate(ArraySegment<byte> buffer, ushort size);

        public abstract void OnConnected(EndPoint endPoint, Session session = null);

        public abstract int OnReceive(ArraySegment<byte> buffer);

        public abstract void OnSend(int numOfBytes);
        
        public virtual void Initialize(string sessionID, Socket clientSocket, ServerConfig config, ILogger logger, List<IListenInfo> listenInfoList, bool isClient)
        {
            mSessionID = sessionID;
            mIsClient = isClient;

            this.logger = logger;

            mServerInfo = config;

            // 통신용 소켓 세팅
            mSocketWrapper = new TcpSocket();
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

        /// <summary>
        /// 비동기 Recv 작업 진행 메서드
        /// - 수신버퍼 초기화 및 SocketAsyncEventArgs의 수신버퍼 세팅
        /// - 비동기 Recv 진행
        /// </summary>
        private void ReceiveAsync()
        {
            // 패킷을 수신할 수 있는 상태인지 확인        
            // Recv SocketAsyncEventArgs null 체크, Socket 상태 Receiving으로 변경  
            if (!CheckStartReceive())
                return;

            // Recv 데이터를 저장할 ArraySegment 버퍼공간 초기화
            mMessageProcessor.Clear();
            // 패킷 메시지를 쓸 수 있는 잔여 수신버퍼 획득
            var segment = mMessageProcessor.GetWriteMessage;
            // 현재 세션에서 수신버퍼의 초기사이즈 만큼의 ArraySegment 배열공간을 SocketAsyncEventArgs 수신버퍼에 세팅. 추후 이 부분에 수신된 패킷 데이터가 담겨질 예정
            mRecvEvent.SetBuffer(segment.Array, segment.Offset, segment.Count);

            // 비동기 Recv 진행 
            var lPending = SocketReceiveAsync();
            if (!lPending)
                OnRecvCompleted(null, mRecvEvent);
        }

        /// <summary>
        /// 비동기 Recv 작업이 성공했을 때 진행되는 Recv 관련 콜백 메서드 
        /// - 수신된 데이터 체크 및 패킷 아이디에 따라 메시지 처리작업 진행
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                // 수신된 바이트 수가 현재 세션에서 관리 중인 수신버퍼의 크기를 초과하였는지 체크
                // 만약, 초과하였다면 잘못된 패킷 데이터가 수신된 것이므로 Recv 상태 제거 및 Socket Close 작업 진행
                if (!mMessageProcessor.OnWriteMessage(e.BytesTransferred))
                {
                    OnReceiveEnd();
                    return;
                }

                // 파라미터의 SocketAsyncEventArgs 객체의 Buffer(수신버퍼)의 내용을 읽어 정상적으로 데이터가 수신되었는지 판단
                // 정상일 경우, 수신버퍼의 내용을 역직렬화를 통해 온전한 패킷으로 만들고 메시지 아이디에 따라서 적절하게 처리진행 
                var processLength = OnReceive(mMessageProcessor.GetReadMessage);
                // OnReceive의 반환값은 현재 읽은 패킷의 사이즈로서 0보다 작거나 현재 읽을 수 있는 크기보다 큰 경우 잘못된 경우로서 Socket Close 작업 진행
                if (processLength < 0 || processLength > mMessageProcessor.GetDataSize)
                {
                    OnReceiveEnd();
                    return;
                }

                // 현재 읽은 패킷 사이즈만큼 수신버퍼의 읽은 사이즈 추가
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
            }
            catch (Exception ex)
            {
                OnReceiveEnd(eCloseReason.SeverException);
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }
        #endregion

        #region "Send Logic"
        /// <summary>
        /// 패킷 send. 여러 곳에서 접근가능
        /// </summary>
        public void StartSend(ArraySegment<byte> message)
        {
            if (message.Array == null)
                return;

            if (!mSocketWrapper.TryAddState(SocketState.Sending))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Socket state is [{mSocketWrapper.GetSocketState}]. Fail to change state [Sending]");
                return;
            }

            lock(mLockObject)
            {
                mSendingQueue.Enqueue(message);
                // SendBufferList에 데이터가 없을 때 현재까지 큐잉된 패킷 데이터를 전송
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

        /// <summary>
        /// 비동기 Send 작업이 성공했을 때 진행되는 Send 관련 콜백 메서드 
        /// 사용한 Send 버퍼리스트 초기화 및 잔여 패킷이 있는 경우 Send 함수 재호출
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">사용자 프로그램에서 운영체제 대상으로 비동기 입출력 IO를 할 수 있도록 도와주는 데이터 교환 등의 가교역할</param>
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
                // 콜백 메서드가 실행되는 스레드는 Send 작업이 호출된 스레드와 다를 수 있으므로 공유 데이터에 대한 Lock 진행
                lock(mLockObject)
                {
                    // Send 버퍼리스트에 있는 대상을 모두 보낸 상태
                    // SocketAsyncEventArgs.BufferList 관련 데이터 초기화
                    mSendEvent.BufferList = null;
                    mSendingList.Clear();

                    OnSend(mSendEvent.BytesTransferred);

                    // Send 큐에 남은 데이터가 있는 경우 SendAsync 메서드 재호출 및 Send 작업 진행
                    if (mSendingQueue.Count > 0)
                    {
                        SendAsync();
                    }
                    else
                    {
                        // 남은 작업이 없는 경우 Socket 상태 Sending 제거 
                        OnSendEnd();

                        // 만약, 이 시점에 Send 큐에 남은 데이터가 있는 경우 SendAsync 메서드 재호출 및 Send 작업 진행
                        if (mSendingQueue.Count > 0)
                            SendAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
                OnSendEnd(false, eCloseReason.SeverException);
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
