using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ServerEngine.Common;
using ServerEngine.Config;
using ServerEngine.Log;
using ServerEngine.Network.Message;
using ServerEngine.Network.SystemLib;
using ServerEngine.Common;
using System.Net.Http.Headers;

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

        #region property
        public long mTokenId { get; protected set; } = 0;
        public bool Connected => mConnected;
        public eTokenType TokenType { get; protected set; } = eTokenType.None;

        protected Channel<ArraySegment<byte>>? SendQueue { get; private set; }
        public SocketBase Socket { get; protected set; }
        public Log.ILogger Logger { get; private set; }

        public SocketAsyncEventArgs? SendAsyncEvent { get; private set; }
        public SocketAsyncEventArgs? RecvAsyncEvent { get; private set; }
        public MessageProcessor? MessageHandler { get; private set; }

        public IPEndPoint? GetLocalEndPoint => mLocalEndPoint;
        public IPEndPoint? GetRemoteEndPoint => mRemoteEndPoint;
        #endregion

        protected bool InitializeBase(Log.ILogger logger, IConfigNetwork config_network, SocketBase socket, SocketAsyncEventArgs send_event_args, SocketAsyncEventArgs recv_event_args)
        {
            this.Socket = socket;
            this.Logger = logger;

            mLocalEndPoint = (IPEndPoint?)socket.GetSocket?.LocalEndPoint;
            mRemoteEndPoint = (IPEndPoint?)socket.GetSocket?.RemoteEndPoint;

            SendAsyncEvent = send_event_args;
            RecvAsyncEvent = recv_event_args;

            var config_socket = config_network.config_socket;

            SendQueue = Channel.CreateBounded<ArraySegment<byte>>(capacity: config_socket.send_queue_size);
            MessageHandler = new MessageProcessor(config_socket.recv_buff_size);

            mConnected = true;

            return true;
        }

        #region public_method
        public virtual void StartSend(ArraySegment<byte> buffer)
        {
            if (null == buffer.Array) 
                throw new ArgumentNullException(nameof(buffer));

            if (null == SendQueue)
                throw new NullReferenceException(nameof(SendQueue));

            if (false == Socket.UpdateState(SocketBase.eSocketState.Sending))
            {
                if (Logger.IsEnableDebug)
                    Logger.Debug($"Debug in UserToken.StartSend() - Fail to update send state [sending]");

                SendQueue.Writer.TryWrite(buffer);
            }

        }

        public virtual ValueTask StartSendAsync(ArraySegment<byte> buffer, CancellationToken cancel_token)
        {
            if (null == buffer.Array) 
                throw new ArgumentNullException(nameof(buffer));

            if (null == SendQueue)
                throw new NullReferenceException(nameof(SendQueue));

            if (false == Socket.UpdateState(SocketBase.eSocketState.Sending))
            {
                if (Logger.IsEnableDebug)
                    Logger.Debug($"Debug in UserToken.StartSendAsync() - Fail to update send state [sending]");
            }

            return SendQueue.Writer.WriteAsync(buffer, cancel_token);
        }

        public virtual async ValueTask SendAsync()
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

        public virtual void StartReceive()
        {

        }

        public virtual void OnRecvCompleteHandler(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                var socket_error = e.SocketError;
                var bytes_transferred = e.BytesTransferred;

                if (false == AsyncCallbackChecker.CheckCallbackHandler(socket_error, bytes_transferred))
                {
                    Logger.Error($"Error in UserToken.OnRecvCompleteHandler() - SocketError = {socket_error}, BytesTransferred = {bytes_transferred}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in UserToken.OnRecvCompleteHandler() - {ex.Message} - {ex.StackTrace}");
                throw;
            }
        }

        public virtual void Dispose()
        {
            if (mDisposed)
                return;

            Socket.Dispose();
            SendAsyncEvent?.Dispose();
            RecvAsyncEvent?.Dispose();

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
