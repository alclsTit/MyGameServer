using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using ServerEngine.Common;
using ServerEngine.Config;
using ServerEngine.Network.Message;
using ServerEngine.Network.SystemLib;
using Google.Protobuf;

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
        private IConfigNetwork? mConfigNetwork;
        private Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, bool>? mRetrieveEvent;

        #region property
        public long mTokenId { get; protected set; } = 0;
        public eTokenType TokenType { get; protected set; } = eTokenType.None;

        protected Channel<ArraySegment<byte>>? SendQueue { get; private set; }
        public SocketBase Socket { get; protected set; }
        public Log.ILogger Logger { get; private set; }

        public SocketAsyncEventArgs? SendAsyncEvent { get; private set; }           // retrieve target
        public SocketAsyncEventArgs? RecvAsyncEvent { get; private set; }           // retrieve target
        public RecvMessageHandler? RecvMessageHandler { get; private set; }         // retrieve target
        public ProtoParser mProtoParser { get; private set; } = new ProtoParser();

        public IPEndPoint? GetLocalEndPoint => mLocalEndPoint;
        public IPEndPoint? GetRemoteEndPoint => mRemoteEndPoint;
        public bool Connected => mConnected;
        #endregion

        protected bool InitializeBase(Log.ILogger logger, IConfigNetwork config_network, SocketBase socket, SocketAsyncEventArgs send_event_args, SocketAsyncEventArgs recv_event_args, RecvStream recv_stream, Func<SocketAsyncEventArgs?, SocketAsyncEventArgs?, bool> retrieve_event)
        {
            this.Socket = socket;
            this.Logger = logger;
            mConfigNetwork = config_network;

            mLocalEndPoint = (IPEndPoint?)socket.GetSocket?.LocalEndPoint;
            mRemoteEndPoint = (IPEndPoint?)socket.GetSocket?.RemoteEndPoint;

            SendAsyncEvent = send_event_args;
            RecvAsyncEvent = recv_event_args;

            var config_socket = config_network.config_socket;

            SendQueue = Channel.CreateBounded<ArraySegment<byte>>(capacity: config_socket.send_queue_size);
            //RecvMessageHandler = new RecvMessageHandler(max_buffer_size: config_socket.recv_buff_size, logger: logger);
            RecvMessageHandler = new RecvMessageHandler(stream: recv_stream, max_buffer_size: config_socket.recv_buff_size, logger: logger);

            mConnected = true;

            mRetrieveEvent = retrieve_event; 

            return true;
        }

        #region public_method
        // protobuf 형식의 메시지를 받아 직렬화한 뒤 비동기로 메시지 큐잉
        public virtual async ValueTask SendAsync<TMessage>(TMessage message, ushort message_id, CancellationToken canel_token) 
            where TMessage : IMessage
        {
            try
            {
                var buffer = mProtoParser.Serialize(message: message, message_id: message_id);
                await StartSendAsync(ref buffer, canel_token);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.SendAsync() - {ex.Message} - {ex.StackTrace}");
                return;
            }
        }

        // protobuf 형식의 메시지를 받아 직렬화한 뒤 동기로 메시지 큐잉
        public virtual bool Send<TMessage>(TMessage message, ushort message_id) 
            where TMessage: IMessage
        {
            try
            {
                var buffer = mProtoParser.Serialize(message: message, message_id: message_id);
                return StartSend(ref buffer);
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.Send() - {ex.Message} - {ex.StackTrace}");
                return false;
            }
        }

        // 여러 스레드에서 호출. 패킷 send 진행 시 큐에 동기로 데이터 추가
        private bool StartSend(ref ArraySegment<byte> buffer)
        {
            if (null == buffer.Array) 
                throw new ArgumentNullException(nameof(buffer));

            if (null == SendQueue)
                throw new NullReferenceException(nameof(SendQueue));

            if (false == Socket.UpdateState(SocketBase.eSocketState.Sending))
                Logger.Error($"Error in UserToken.StartSend() - Fail to update send state [sending]");

            return SendQueue.Writer.TryWrite(buffer);
        }

        // 여러 스레드에서 호출. 패킷 send 진행 시 큐에 비동기로 데이터 추가
        private ValueTask StartSendAsync(ref ArraySegment<byte> buffer, CancellationToken cancel_token)
        {
            if (null == buffer.Array) 
                throw new ArgumentNullException(nameof(buffer));

            if (null == SendQueue)
                throw new NullReferenceException(nameof(SendQueue));

            if (false == Socket.UpdateState(SocketBase.eSocketState.Sending))
                Logger.Error($"Error in UserToken.StartSendAsync() - Fail to update send state [sending]");

            return SendQueue.Writer.WriteAsync(buffer, cancel_token);
        }

        // 별도의 패킷 처리 스레드에서 호출. 큐잉된 패킷 데이터들에 대한 실질적인 비동기 send 진행
        public virtual async ValueTask ProcessSendAsync()
        {
            if (false == Connected)
                return;

            if (null == SendQueue)
                return;

            if (null == SendAsyncEvent)
                return;

            SendQueue.Writer.Complete();
            await foreach(var item in SendQueue.Reader.ReadAllAsync())
            {
                SendAsyncEvent.BufferList?.Add(item);
            }

            var pending = Socket.GetSocket?.SendAsync(SendAsyncEvent);
            if (false == pending)
            {
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


            }
            catch (Exception ex) 
            {
                Logger.Error($"Exception in UserToken.OnSendCompleteHandler() - {ex.Message} - {ex.StackTrace}", ex);
            }
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
                    Logger.Error($"Error in UserToken.StartReceive() - Buffer Set Error");
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

            // Dispose (close) socket
            Socket.DisconnectSocket();

            // Dispose send/recv socketasynceventargs
            mRetrieveEvent?.Invoke(SendAsyncEvent, RecvAsyncEvent);

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
