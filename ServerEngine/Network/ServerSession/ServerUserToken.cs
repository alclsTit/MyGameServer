using ServerEngine.Network.SystemLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.ServerSession
{
    public class ServerUserToken : UserToken
    {
        private IPEndPoint? mLocalEndPoint;
        private IPEndPoint? mRemoteEndPoint;

        public ServerUserToken(SocketBase socket, bool client_connect = false)
            : base(socket)
        {
            IsClientConnect = client_connect;


        }
    }
}
