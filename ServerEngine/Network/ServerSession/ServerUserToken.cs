using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.ServerSession
{
    public class ServerUserToken : UserToken
    {
        public ServerUserToken(bool client_connect = false)
            : base()
        {
            IsClientConnect = client_connect;
        }
    }
}
