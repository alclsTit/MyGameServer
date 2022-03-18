using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.Server
{
    /// <summary>
    /// AppServer 위에서 여러대의 SocketServer 구동
    /// 각각의 SocketServer는 Listener 를 가지고 있음. 서버의 모든 작업은 SocketServer에서 진행
    /// </summary>
    public class ServerAcceptModule : ServerModuleBase
    {
        public ServerAcceptModule() : base()
        {

        }
    }

    public class ServerConnectModule : ServerModuleBase
    {
        public ServerConnectModule() : base()
        {

        }
    }

}

