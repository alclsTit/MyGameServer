using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ServerEngine.Config;
using ServerEngine.Log;
using ServerEngine.Network.Message;
using ServerEngine.Network.SystemLib;

namespace ServerEngine.Network.ServerSession
{
    public class UserTokenManager
    {
        #region Lazy Singletone
        public static readonly Lazy<UserTokenManager> m_instance = new Lazy<UserTokenManager>(() => new UserTokenManager());
        public static UserTokenManager Instance => m_instance.Value;
        private UserTokenManager() {}
        #endregion

        private ConcurrentDictionary<int, List<UserToken>> mThreadUserTokens = new ConcurrentDictionary<int, List<UserToken>>();                      // key : thread_index 
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

    public abstract class UserToken
    {
        public enum eTokenType
        {
            None = 0,
            Client,
            Server
        }

        #region property
        public bool Connected { get; protected set; }
        public eTokenType TokenType { get; protected set; }

        protected Channel<ArraySegment<byte>> SendQueue { get; private set; }
        public SocketBase Socket { get; protected set; }
        public IPEndPoint RemoteEndPoint { get; protected set; }
        public IPEndPoint LocalEndPoint { get; protected set; }
        public Log.ILogger Logger { get; private set; }

        public SocketAsyncEventArgs RecvAsyncEvent { get; private set; }
        public SocketAsyncEventArgs SendAsyncEvent { get; private set; }
        public MessageProcessor MessageHandler { get; private set; }
        #endregion

        protected UserToken(SocketBase socket, EndPoint remote_endpoint, EndPoint local_endpoint, eTokenType type, Log.ILogger logger, IConfigNetwork config_network)
        {
            this.Socket = socket;
            this.Logger = logger;   

            TokenType = type;
            LocalEndPoint = (IPEndPoint)local_endpoint;
            RemoteEndPoint = (IPEndPoint)remote_endpoint;

            var config_socket = config_network.config_socket;

            SendQueue = Channel.CreateBounded<ArraySegment<byte>>(capacity: config_socket.send_queue_size);
            MessageHandler = new MessageProcessor(config_socket.recv_buff_size);
        }

        public virtual void StartSend(ArraySegment<byte> buffer)
        {
            if (null == buffer.Array) 
                throw new ArgumentNullException(nameof(buffer));

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

            if (false == Socket.UpdateState(SocketBase.eSocketState.Sending))
            {
                if (Logger.IsEnableDebug)
                    Logger.Debug($"Debug in UserToken.StartSendAsync() - Fail to update send state [sending]");
            }

            return SendQueue.Writer.WriteAsync(buffer, cancel_token);
        }

        public virtual async ValueTask SendAsync()
        {
            SendQueue.Writer.Complete();
            await foreach(var item in SendQueue.Reader.ReadAllAsync())
            {
                SendAsyncEvent.BufferList?.Add(item);
            }

            var pending = Socket.GetSocket?.SendAsync(SendAsyncEvent);
            if (false == pending)
            {

            }
        }

        public virtual void Dispose()
        {
            Socket.Dispose();

            Connected = false;
            TokenType = eTokenType.None;
        }
    }
}
