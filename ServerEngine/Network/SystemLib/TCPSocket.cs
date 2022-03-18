using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using ServerEngine.Common;
using ServerEngine.Config;
using ServerEngine.Log;

namespace ServerEngine.Network.SystemLib
{
    public class TCPSocket : SocketBase
    {
        public void Initialize(Socket clientSocket, ServerConfig config, Logger logger)
        {
            base.logger = logger;
            mRawSocket = clientSocket;
            SetSocketOption(config);
        }

        /// <summary>
        /// TCP Socket 옵션 세팅하는 부분. 일단 config 파일을 통한 내부에서만 옵션 세팅 가능하도록 작업
        /// </summary>
        /// <param name="config"></param>
        private void SetSocketOption(ServerConfig config)
        {
            if (config.nodelay)
                mRawSocket.NoDelay = true;

            mRawSocket.ReceiveBufferSize = config.recvBufferSize > 0 ? config.recvBufferSize : config.DefaultRecvBufferSize;

            mRawSocket.SendBufferSize = config.sendBufferSize > 0 ? config.sendBufferSize : config.DefaultSendBufferSize;

            if (config.socketLingerFlag)
            {
                var lingerDelayTime = config.socketLingerDelayTime > 0 ? config.socketLingerDelayTime : config.DefaultSocketLingerDelayTime;
                mRawSocket.LingerState = new LingerOption(true, lingerDelayTime);
            }

            mRawSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

    }
}
