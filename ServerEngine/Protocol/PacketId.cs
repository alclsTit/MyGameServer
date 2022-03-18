using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Protocol
{
    public enum ePacketId : ushort
    {
        notify_dummy_packet = 0,
        notify_socket_session_connected = 1,
        notify_socket_session_disconnected = 2
    }
}
