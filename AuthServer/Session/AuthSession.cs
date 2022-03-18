using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using ServerEngine.Network.ServerSession;


namespace AuthServer
{
    public class AuthSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint, Session session = null)
        {
            var connectedEndPoint = (IPEndPoint)endPoint;

            Console.WriteLine($"[Session]: Connected to Server[{connectedEndPoint.Address.ToString()}:{connectedEndPoint.Port}]");
        }
    }
}
