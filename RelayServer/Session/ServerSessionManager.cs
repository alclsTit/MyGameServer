using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using ServerEngine.Network.ServerSession;

namespace RelayServer
{
    /// <summary>
    /// 모든 Server에 대한 Session 관리
    /// </summary>
    internal class ServerSessionManager : SessionManagerBase
    {
        public override void Initialize(int maxConnect)
        {
            base.Initialize(maxConnect);
        }
    }

}
