using ServerEngine.Network.SystemLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.ServerSession
{
    public class ClientUserToken : UserToken
    {
        public ClientUserToken(SocketBase socket, bool client_connect = true)
            : base(socket)
        {
            IsClientConnect = client_connect;

        }
    }
}
