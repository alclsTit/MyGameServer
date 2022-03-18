using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

using ServerEngine.Protocol;
using ServerEngine.Network.ServerSession;
using ServerEngine.Network.Message;

namespace RelayServer
{
    public class RelaySession : PacketSession
    {
        /// <summary>
        /// 클라이언트와 서버간 Connect, Accept 이후 Session이 생성된 이후의 작업
        /// </summary>
        /// <param name="endPoint"></param>
        public override void OnConnected(EndPoint endPoint, Session session = null)
        {
            var connectedEndPoint = (IPEndPoint)endPoint;

            var isClient = session.mIsClient;
            if (isClient)
            {

            }
            else
            {
                // 서버 세션 접속
                var id = session.mSessionID;
                switch (id.ToLower().Trim())
                {
                    // RelayServer와 AuthServer 서버간 연결 완료
                    case "authserver":
                        var notify_msg = new Message_World.notify_socket_session_connected();
                        notify_msg.remoteIP = connectedEndPoint.Address.ToString();
                        notify_msg.sessionID = mSessionID;
                        Relay(notify_msg);
                        break;
                    // RelayServer와 Matchroom 서버간 연결 완료
                    case "mathroomserver":

                    default:
                        break;
                }

                if (logger.IsDebugEnabled)
                    Console.WriteLine($"[Session]: Connected Server[{id}] to CS_Server[{connectedEndPoint.Address}:{connectedEndPoint.Port}]");

            }

            //Thread.Sleep(3000);
            //OnReceiveEnd();

            // Connected 된 패킷이 온 곳이 클라이언트인지 서버인지 판별 
        }

    }
}
