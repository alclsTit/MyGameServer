using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Network.ServerSession;

namespace AuthServer
{
    internal class ServerSessionManager : SessionManagerBase
    { 
        public override void Initialize(int maxConnect)
        {
            base.Initialize(maxConnect);
        }
    }
}
