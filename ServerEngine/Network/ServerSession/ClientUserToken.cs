using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.ServerSession
{
    public class ClientUserToken : UserToken
    {
        public ClientUserToken(bool client_connect = true)
            : base()
        {
            IsClientConnect = client_connect;
        }
    }
}
