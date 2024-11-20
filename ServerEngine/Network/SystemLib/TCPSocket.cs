using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using ServerEngine.Common;
using ServerEngine.Config;
using ServerEngine.Log;
using System.Configuration;

namespace ServerEngine.Network.SystemLib
{
    public class TcpSocket : SocketBase
    {
        public TcpSocket(Socket socket, Log.ILogger logger) 
            : base(socket: socket, logger: logger)
        {
        }

        public override void SetSocketOption(IConfigNetwork config_network)
        {
            if (null == config_network)
                throw new ArgumentNullException(nameof(config_network));

            if (null == mRawSocket)
                throw new NullReferenceException(nameof(mRawSocket));

            var config_socket               = config_network.config_socket;

            if (null == config_socket)
                throw new ArgumentNullException(nameof(config_socket));

            mRawSocket.NoDelay              = 0 != config_socket.no_delay;
            mRawSocket.ReceiveBufferSize    = config_socket.recv_buff_size;
            mRawSocket.SendBufferSize       = config_socket.send_buff_size;
            mRawSocket.ReceiveTimeout       = config_socket.recv_timeout;
            mRawSocket.SendTimeout          = config_socket.send_timeout;

            if (config_socket.linger_time < 0)
                throw new ArgumentOutOfRangeException(nameof(config_socket.linger_time));

            mRawSocket.LingerState          = new LingerOption(config_socket.linger_time > 0, config_socket.linger_time);

            try
            {
                // keep-alive option operates if tcp-socket
                mRawSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                mRawSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, config_socket.heartbeat_start_time);
                mRawSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, config_socket.heartbeat_check_time);
                mRawSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, config_socket.heartbeat_count);
            }
            catch (Exception e)
            {
                Logger.Error($"Exception in TcpSocket.SetSocketOption - {e.Message} - {e.StackTrace}");
            }
        }
    }
}
