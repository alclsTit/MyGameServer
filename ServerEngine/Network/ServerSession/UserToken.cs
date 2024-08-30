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
        public static readonly Lazy<UserTokenManager> m_instance = new Lazy<UserTokenManager>(() => new UserTokenManager());
        public static UserTokenManager Instance => m_instance.Value;
        private UserTokenManager() { }

        public ConcurrentDictionary<long, UserToken> UserTokenList { get; private set; } = new ConcurrentDictionary<long, UserToken>();
        
        public void Add(long key, UserToken token)
        {

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
        public ILogger Logger { get; private set; }

        public SocketAsyncEventArgs RecvAsyncEvent { get; private set; }
        public SocketAsyncEventArgs SendAsyncEvent { get; private set; }
        public MessageProcessor MessageHandler { get; private set; }
        #endregion

        protected UserToken(SocketBase socket, EndPoint remote_endpoint, EndPoint local_endpoint, eTokenType type, ILogger logger, IConfigNetwork config_network)
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

        public virtual ValueTask StartSendAsync(ArraySegment<byte> buffer)
        {
            if (null == buffer.Array) 
                throw new ArgumentNullException(nameof(buffer));

            if (false == Socket.UpdateState(SocketBase.eSocketState.Sending))
            {
                if (Logger.IsEnableDebug)
                    Logger.Debug($"Debug in UserToken.StartSendAsync() - Fail to update send state [sending]");

                return SendQueue.Writer.WriteAsync(buffer);
            }
            else
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
