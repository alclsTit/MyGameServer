using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using ServerEngine.Common;
using ServerEngine.Config;
using ServerEngine.Network.Message;
using ServerEngine.Network.SystemLib;
using Google.Protobuf;
using System.Collections.Concurrent;
using Serilog.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace ServerEngine.Network.ServerSession
{
    public abstract class UserTokenManager
    {
        protected Log.ILogger Logger;
        protected IConfigNetwork m_config_network;

        protected UserTokenManager(Log.ILogger logger, IConfigNetwork config_network)
        {
            this.Logger = logger;
            m_config_network = config_network;
        }
    }

    /*public class UserTokenManager
    {
        #region Lazy Singletone
        public static readonly Lazy<UserTokenManager> m_instance = new Lazy<UserTokenManager>(() => new UserTokenManager());
        public static UserTokenManager Instance => m_instance.Value;
        private UserTokenManager() {}
        #endregion

        private ConcurrentDictionary<int, List<UserToken>> mThreadUserTokens = new ConcurrentDictionary<int, List<UserToken>>();            // key : thread_index 
        public ConcurrentDictionary<long, UserToken> UserTokens { get; private set; } = new ConcurrentDictionary<long, UserToken>();        // key : uid
        
        private Log.ILogger? Logger;
        private IConfigNetwork? m_config_network;

        public bool Initialize(Log.ILogger logger, IConfigNetwork config_network)
        {
            this.Logger = logger;
            m_config_network = config_network;

            for (var i = 0; i < config_network.max_io_thread_count; ++i) 
                mThreadUserTokens.TryAdd(i, new List<UserToken>(1000));

            return true;
        }

        public bool TryAddUserToken(long uid, UserToken token)
        {
            if (0 >= uid)
                throw new ArgumentException(nameof(uid));

            if (null == token)
                throw new ArgumentNullException(nameof(token));

            if (null == m_config_network) 
                throw new NullReferenceException(nameof(m_config_network));

            var index = (int)(uid % m_config_network.max_io_thread_count);
            mThreadUserTokens[index].Add(token);

            return UserTokens.TryAdd(uid, token);
        }

        public async ValueTask Run(int index)
        {
            if (null == m_config_network)
                throw new NullReferenceException(nameof(m_config_network));

            if (0 > index || m_config_network.max_io_thread_count <= index)
                throw new ArgumentException($"Index {index}");

            while(true)
            {
                foreach(var token in mThreadUserTokens[index])
                {
                    await token.SendAsync();
                }

                Thread.Sleep(10);
            }
        }
    }
    */

    public abstract class UserToken : IDisposable
    {
        public enum eTokenType
        {
            None = 0,
            Client,
            Server
        }

        private IPEndPoint? mLocalEndPoint;
        private IPEndPoint? mRemoteEndPoint;
        private bool mDisposed = false;
        private volatile bool mConnected = false;
        private IConfigSocket? mConfigSocket;
        private IConfigNetwork? mConfigNetwork;
        private volatile int mCompleteFlag = 0; // 0: false / 1: true
        private ConcurrentQueue<SendStream> mSendBackupQueue = new ConcurrentQueue<SendStream>();
        private Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, SendStreamPool?, bool>? mRetrieveEvent;
        protected System.Threading.Timer? mBackgroundTimer = null;
        protected long mHeartbeatCheckTime, mLastHeartbeatCheckTime;
        protected volatile int mHeartbeatCount;

        #region property
        public long mTokenId { get; protected set; } = 0;
        public eTokenType TokenType { get; protected set; } = eTokenType.None;
        protected Channel<SendStream>? SendQueue { get; private set; }
        public SocketBase Socket { get; protected set; }
        public Log.ILogger Logger { get; private set; }

        public SocketAsyncEventArgs? SendAsyncEvent { get; private set; }           // retrieve target
        public SocketAsyncEventArgs? RecvAsyncEvent { get; private set; }           // retrieve target

        // UserToken : 5000, pool_size = 10, send_buffer_size = 4KB > 서버당 204MB
        public SendStreamPool? SendStreamPool { get; private set; }         
     
        public RecvMessageHandler? RecvMessageHandler { get; private set; }         // retrieve target
        public ProtoParser mProtoParser { get; private set; } = new ProtoParser();

        public IPEndPoint? GetLocalEndPoint => mLocalEndPoint;
        public IPEndPoint? GetRemoteEndPoint => mRemoteEndPoint;
        public bool Connected => mConnected;

        public IConfigNetwork? GetConfigNetwork => mConfigNetwork;
        public IConfigSocket? GetConfigSocket => mConfigSocket;

        #endregion

        public abstract void HeartbeatCheck(object? state);

        protected bool InitializeBase(Log.ILogger logger, IConfigNetwork config_network, SocketBase socket, 
                                      SocketAsyncEventArgs send_event_args, SocketAsyncEventArgs recv_event_args, 
                                      SendStreamPool send_stream_pool, RecvStream recv_stream, 
                                      Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, SendStreamPool?, bool> retrieve_event)
        {
            this.Socket = socket;
            this.Logger = logger;
            mConfigNetwork = config_network;

            mLocalEndPoint = (IPEndPoint?)socket.GetSocket?.LocalEndPoint;
            mRemoteEndPoint = (IPEndPoint?)socket.GetSocket?.RemoteEndPoint;

            SendAsyncEvent = send_event_args;
            RecvAsyncEvent = recv_event_args;

            mConfigSocket = config_network.config_socket;

            SendQueue = Channel.CreateBounded<SendStream>(capacity: config_network.config_socket.send_queue_size);
            SendStreamPool = send_stream_pool;

            //RecvMessageHandler = new RecvMessageHandler(max_buffer_size: config_socket.recv_buff_size, logger: logger);
            RecvMessageHandler = new RecvMessageHandler(stream: recv_stream, max_buffer_size: config_network.config_socket.recv_buff_size, logger: logger);

            mConnected = true;

            mRetrieveEvent = retrieve_event; 

            return true;
        }

        #region public_method
        public (string, int) GetRemoteEndPointIPAddress()
        {
            if (null == mRemoteEndPoint)
                return default;

            if (null == mRemoteEndPoint.Address)
                return default;

            return (mRemoteEndPoint.Address.ToString(), mRemoteEndPoint.Port);
        }

        // 클라이언트 or 서버로부터 전달받은 heartbeat 패킷 시간
        public void SetHeartbeatCheckTime(long time)
        {
            if (0 != mHeartbeatCheckTime)
                mLastHeartbeatCheckTime = mHeartbeatCheckTime;

            mHeartbeatCheckTime = time;
        }

        // protobuf 형식의 메시지를 받아 직렬화한 뒤 동기로 메시지 큐잉
        public virtual bool Send<TMessage>(TMessage message, ushort message_id)
            where TMessage : IMessage
        {
            if (null == SendStreamPool)
            {
                Logger.Error($"Error in UserToken.Send() - SendStreamPool is null");
                return false;
            }

            try
            {
                SendStream stream = SendStreamPool.Get();
                if (mProtoParser.TrySerialize(message: message,
                                              message_id: message_id,
                                              stream: stream))
                {
                    return StartSend(stream);
                }
                else
                {
                    Logger.Error($"Error in UserToken.Send() - Fail to Serialize");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.Send() - {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }

        // protobuf 형식의 메시지를 받아 직렬화한 뒤 비동기로 메시지 큐잉
        public virtual async ValueTask SendAsync<TMessage>(TMessage message, ushort message_id, CancellationToken canel_token) 
            where TMessage : IMessage
        {
            if (null == SendStreamPool)
            {
                Logger.Error($"Error in UserToken.SendAsync() - SendStreamPool is null");
                return;
            }

            try
            {
                SendStream stream = SendStreamPool.Get();
                if (mProtoParser.TrySerialize(message: message,
                                              message_id: message_id,        
                                              stream: stream))
                {
                    await StartSendAsync(stream: stream, cancel_token: canel_token);
                }
                else
                {
                    Logger.Error($"Error in UserToken.SendAsync() - Fail to Serialize");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.SendAsync() - {ex.Message} - {ex.StackTrace}");
                return;
            }
        }

        // 여러 스레드에서 호출. 패킷 send 진행 시 큐에 동기로 데이터 추가
        // SocketAsyncEventArgs 비동기 객체를 사용하지 않음
        private bool StartSend(SendStream stream)
        {
            if (null == stream.Buffer.Array)
                throw new ArgumentNullException(nameof(stream.Buffer.Array));

            if (null == SendQueue)
                throw new NullReferenceException(nameof(SendQueue));

            if (!Socket.UpdateState(SocketBase.eSocketState.Sending))
                Logger.Error($"Error in UserToken.StartSend() - Fail to update send state [sending]");

            // mCompleteFlag = false 일 때만 queue에 데이터 추가
            if (0 == Interlocked.CompareExchange(ref mCompleteFlag, 0, 0))
            {
                while(!mSendBackupQueue.IsEmpty)
                {
                    SendStream? backup_stream = null;
                    if (mSendBackupQueue.TryDequeue(out backup_stream))
                    {
                        if (!SendQueue.Writer.TryWrite(item: backup_stream))
                        {
                            Logger.Warn($"Warning in UserToken.StartSend() - Fail to Channel.Writer.TryWrite()." +
                                        $" backup_stream = {Newtonsoft.Json.JsonConvert.SerializeObject(backup_stream, Newtonsoft.Json.Formatting.Indented)}");

                            Task.Delay(5).Wait();
                            mSendBackupQueue.Enqueue(backup_stream);
                            continue;
                        }
                    }
                }

                if (!SendQueue.Writer.TryWrite(item: stream))
                {
                    // channel에 데이터 추가가 실패하면 backup queue에 추가하여 이후에 처리한다
                    mSendBackupQueue.Enqueue(stream);
                    
                    Logger.Warn($"Warning in UserToken.StartSend() - Fail to Channel.Writer.TryWrite()." +
                                $" stream = {Newtonsoft.Json.JsonConvert.SerializeObject(stream, Newtonsoft.Json.Formatting.Indented)}");               
                }
            }
            else
            {
                mSendBackupQueue.Enqueue(stream);
            }
            
            return true;
        }

        // 여러 스레드에서 호출. 패킷 send 진행 시 큐에 비동기로 데이터 추가
        // SocketAsyncEventArgs 비동기 객체를 사용하지 않음
        private ValueTask<bool> StartSendAsync(SendStream stream, CancellationToken cancel_token)
        {
            if (null == stream.Buffer.Array)
                throw new ArgumentNullException(nameof(stream.Buffer.Array));

            if (null == SendQueue)
                throw new NullReferenceException(nameof(SendQueue));

            if (!Socket.UpdateState(SocketBase.eSocketState.Sending))
                Logger.Error($"Error in UserToken.StartSendAsync() - Fail to update send state [sending]");

            // mCompleteFlag = false 일 때만 queue에 데이터 추가
            if (0 == Interlocked.CompareExchange(ref mCompleteFlag, 0, 0))
            { 
                while(!mSendBackupQueue.IsEmpty)
                {
                    SendStream? backup_stream;
                    if (mSendBackupQueue.TryPeek(out backup_stream))
                    {
                        try
                        {
                            SendQueue.Writer.WriteAsync(item: backup_stream, cancellationToken: cancel_token);
                            
                            mSendBackupQueue.TryDequeue(out backup_stream);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"Warning in UserToken.StartSendAsync() - Fail to Channel.Writer.WriteAsync(). exception = {ex.Message}." +
                                        $"backup_stream = {Newtonsoft.Json.JsonConvert.SerializeObject(backup_stream, Newtonsoft.Json.Formatting.Indented)}. stack_trace = {ex.StackTrace}");

                            Task.Delay(5).WaitAsync(cancellationToken: cancel_token);
                        }
                    }
                }

                try
                {
                    SendQueue.Writer.WriteAsync(item: stream, cancellationToken: cancel_token);
                }
                catch (Exception ex)
                {
                    mSendBackupQueue.Enqueue(item: stream);
                    Logger.Warn($"Warning in UserToken.StartSendAsync() - Fail to Channel.Writer.WriteAsync(). exception = {ex.Message}. " +
                                $"stream = {Newtonsoft.Json.JsonConvert.SerializeObject(stream, Newtonsoft.Json.Formatting.Indented)}. stack_trace = {ex.StackTrace}");
                }
            }
            else
            {
                mSendBackupQueue.Enqueue(stream);
            }

            return ValueTask.FromResult(true);
        }

        // 별도의 패킷 처리 스레드에서 호출. 큐잉된 패킷 데이터들에 대한 실질적인 비동기 send 진행
        // io_thread가 5개일 경우 5개의 스레드에서 각 호출
        public virtual async ValueTask ProcessSendAsync()
        {
            if (false == Connected)
                return;

            if (null == SendQueue)
                return;

            if (null == SendAsyncEvent)
                return;

            // 해당 메서드가 호출되는 순간 Channel.Writer를 Complete로 변경. 더 이상 queue에 데이터 추가가 안됨
            if (0 == Interlocked.CompareExchange(ref mCompleteFlag, 1, 0))
            {
                SendQueue.Writer.Complete();
           
                await foreach(var item in SendQueue.Reader.ReadAllAsync())
                    SendAsyncEvent.BufferList?.Add(item.Buffer);

                //channel의 complete 호출시 해당 channel을 재사용하는것이 아닌 새로운 channel을 생성하여 이후로직을 처리
                //mCompleteFlag = 0;
                if (null != mConfigSocket)
                    SendQueue = Channel.CreateBounded<SendStream>(capacity: mConfigSocket.send_buff_size);
                else
                    SendQueue = Channel.CreateBounded<SendStream>(capacity: Utility.MAX_SEND_BUFFER_SIZE_COMMON);

                var pending = Socket.GetSocket?.SendAsync(SendAsyncEvent);
                if (false == pending)
                    OnSendCompleteHandler(null, SendAsyncEvent);
            }
        }

        public virtual void OnSendCompleteHandler(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                var socket_error = e.SocketError;
                var bytes_transferred = e.BytesTransferred;

                if (false == AsyncCallbackChecker.CheckCallbackHandler(socket_error, bytes_transferred))
                {
                    Logger.Error($"Error in UserToken.OnSendCompleteHandler() - SocketError = {socket_error}, BytesTransferred = {bytes_transferred}");
                    return;
                }

                // 네트워크 상태이상으로 한번에 보내지지 못한 미처리패킷에 대한 후처리 진행
                if (null != e.BufferList)
                {
                    // e.BytesTransferred(실제 전송한 바이트 수)가 BufferList의 각 사이즈 합계(버퍼리스트 총 바이트 수)보다 작다면 재전송
                    int buffer_bytes_transferred = e.BufferList.Sum(buffer => buffer.Count);

                    if (bytes_transferred < buffer_bytes_transferred)
                    {
                        Logger.Warn($"Warning in UserToken.OnSendCompleteHandler() - Partial send detected. Transferred {bytes_transferred} bytes, expected {buffer_bytes_transferred}");

                        List<ArraySegment<byte>> remaining_buffers = GetRemainingBuffers(buffer_list: e.BufferList, bytesTransferred: bytes_transferred);

                        e.BufferList = remaining_buffers;

                        var pending = Socket.GetSocket?.SendAsync(e);
                        if (false == pending)
                            OnSendCompleteHandler(sender, e);
                    }
                    else
                    {
                        // 모든 데이터가 정상적으로 전달
                        if (Logger.IsEnableDebug)
                            Logger.Debug($"All data sent successfully. Transferred {bytes_transferred} bytes. expected {buffer_bytes_transferred}");
                    }
                }
                else
                {
                    Logger.Error($"Error in UserToken.OnSendCompleteHandler() - Send BufferList is Empty. e.BufferList is null");
                }
            }
            catch (Exception ex) 
            {
                Logger.Error($"Exception in UserToken.OnSendCompleteHandler() - {ex.Message} - {ex.StackTrace}", ex);
            }
        }

        /// <summary>
        /// 잔여 Send 패킷으로 인한 재전송이 필요한 경우, 재전송 버퍼체크 및 반환
        /// </summary>
        /// <param name="buffer_list">e.BufferList</param>
        /// <param name="bytesTransferred">e.bytesTransferred</param>
        /// <returns></returns>
        private List<ArraySegment<byte>> GetRemainingBuffers(IList<ArraySegment<byte>> buffer_list, int bytesTransferred)
        {
            List<ArraySegment<byte>> remaining_buffers = new List<ArraySegment<byte>>();
            int total_transferred = 0;

            // buffer[0] : 14
            // buffer[1] : 17
            // buffer[2] : 20  ---- transferred : 51 / 50
            // buffer[3] : 11
            // buffer[4] : 100

            foreach(var buffer in buffer_list) 
            {
                if (null == buffer.Array)
                    continue;

                if (total_transferred + buffer.Count <= bytesTransferred)
                {
                    total_transferred += buffer.Count;
                    continue;
                }

                int remaining_buffer_count = bytesTransferred - total_transferred;
                if (remaining_buffer_count > 0)
                {
                    // offset : buffer.offset + (이미 전송된 바이트 수)
                    // count : 남이있는 바이트 수 >> 세그먼트의 전체 바이트 수 - (이미 전송된 바이트 수)
                    remaining_buffers.Add(new ArraySegment<byte>(buffer.Array, buffer.Offset + remaining_buffer_count, buffer.Count - remaining_buffer_count));
                }
                else
                {
                    // 아예 전송되지 않은 버퍼의 경우
                    remaining_buffers.Add(buffer);
                }

                total_transferred += buffer.Count;
            }

            return remaining_buffers;
        }

        public virtual void StartReceive(/*RecvStream stream*/)
        {
            if (false == Socket.IsNullSocket())
            {
                Logger.Error($"Error in UserToken.StartReceive() - Socket is null");
                return;
            }

            if (false == Socket.IsConnected())
            {
                Logger.Error($"Error in UserToken.StartReceive() - Socket is not connected");
                return;
            }

            if (null == RecvAsyncEvent)
            {
                Logger.Error($"Error in UserToken.StartReceive() - RecvAsyncEvent is null");
                return;
            }

            if (false == Socket.UpdateState(SocketBase.eSocketState.Recving))
            {
                Logger.Error($"Error in UserToken.StartReceive() - Fail to update recv state [recving]");
                return;
            }

            try
            {
                // RecvStreamPool에서 해당 UserToken 전용 RecvStream을 할당
                // session 만료 or socket disconnect 시, Pool에 반환
                var buffer = RecvMessageHandler?.GetBuffer;
                if (buffer.HasValue)
                {
                    RecvAsyncEvent.SetBuffer(buffer: buffer.Value.Array, offset: buffer.Value.Offset, count: buffer.Value.Count);
                    var pending = Socket.GetSocket?.ReceiveAsync(e: RecvAsyncEvent);
                    if (false == pending)
                        OnRecvCompleteHandler(null, e: RecvAsyncEvent);
                }
                else
                {
                    Logger.Error($"Error in UserToken.StartReceive() - RecvAsyncEvent.Buffer Set Error");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.StartReceive() - {ex.Message} - {ex.StackTrace}");
                return;
            }
        }

        public virtual void OnRecvCompleteHandler(object? sender, SocketAsyncEventArgs e)
        {
            var socket_error = e.SocketError;
            var bytes_transferred = e.BytesTransferred;

            if (false == AsyncCallbackChecker.CheckCallbackHandler(socket_error, bytes_transferred))
            {
                Logger.Error($"Error in UserToken.OnRecvCompleteHandler() - SocketError = {socket_error}, BytesTransferred = {bytes_transferred}");
                return;
            }

            if (null == RecvMessageHandler)
            {
                Logger.Error($"Error in UserToken.OnRecvCompleteHandler() - RecvMessageHandler is null");
                return;
            }

            Socket.RemoveState(SocketBase.eSocketState.Recving);

            try
            {
                if (false == RecvMessageHandler.WriteMessage(bytes_transferred))
                {
                    Logger.Error($"Error in UserToken.OnRecvCompleteHandler() - Buffer Write Error");
                    return;
                }

                ArraySegment<byte> recv_buffer;
                var read_result = RecvMessageHandler.TryGetReadBuffer(out recv_buffer);
                if (false == read_result && null != mConfigNetwork)
                {
                    int max_recv_buffer = mConfigNetwork.config_socket.recv_buff_size;
                    RecvMessageHandler.ResetBuffer(new RecvStream(buffer_size: max_recv_buffer), max_recv_buffer);

                    RecvMessageHandler.TryGetReadBuffer(out recv_buffer);

                    Logger.Warn($"Warning in UserToken.OnRecvCompleteHandler() - RecvStream is created by new allocator");
                }

                var process_length = RecvMessageHandler?.ProcessReceive(recv_buffer);
                if (false == process_length.HasValue || 
                    0 > process_length || RecvMessageHandler?.GetHaveToReadSize < process_length)
                {
                    Logger.Error($"Error in UserToken.OnRecvCompleteHandler() - Buffer Processing Error. Read bytes = {process_length}");
                    return; 
                }


                if (false == RecvMessageHandler?.ReadMessage(process_length.Value))
                {
                    Logger.Error($"Error in UserToken.OnRecvCompleteHandler() - Buffer Read Error");
                    return;
                }

                StartReceive();

            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.OnRecvCompleteHandler() - {ex.Message} - {ex.StackTrace}");
                return;
            }
        }

        public virtual void ProcessClose(bool force_close)
        {
            try
            {
                // 1. null socket check (null > exit)
                // 2. socket close check (closed or closing > exit)
                // 3. socket connect check (false > exit)
                if (true == Socket.IsNullSocket() || 
                    true == Socket.IsClosed() || 
                    false == Socket.IsConnected())
                {
                    return;
                }

                var socket_state = SocketBase.eSocketState.Closing;
                if (false == Socket.UpdateState(SocketBase.eSocketState.Closing))
                {
                    Logger.Error($"Error in UserToken.ProcessEnd() - Fail to update socket state {socket_state}. token_id = {mTokenId}, token_type = {TokenType}");
                    return;
                }

                // connected && not closing, close complete
                bool sending = Socket.CheckState(SocketBase.eSocketState.Sending);
                bool recving = Socket.CheckState(SocketBase.eSocketState.Recving);

                if (sending && recving)
                {
                    // socket sending and recving
                    // Todo : SendQueue에 보내야할 데이터가 남아있을 때 처리작업 필요
                   

                }
                else if (sending)
                {
                    // socket sending
                }
                else if (recving)
                {
                    // socket recving
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.ProcessEnd() - {ex.Message} - {ex.StackTrace}");
                return;
            }
        }

        public virtual void Dispose()
        {
            if (mDisposed)
                return;

            // Dispose timer
            mBackgroundTimer?.Dispose();

            // Dispose (close) socket
            Socket.DisconnectSocket();

            // Dispose send/recv socketasynceventargs
            mRetrieveEvent?.Invoke(SendAsyncEvent, RecvAsyncEvent, SendStreamPool);

            mConnected = false;
            TokenType = eTokenType.None;

            // Todo : SendQueue에 보내야할 데이터가 남아있을 때 처리작업 필요
            /*if (0 < SendQueue.Reader.Count)
            {
                await SendAsync();
            }
            */

            mDisposed = true;
        }
        #endregion
    }
}
