using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Network.Message;

namespace AuthServer.Handler
{
    internal class MessageReceiver
    {
        // 해당 프로세스에서 응답 패킷으로 사용할 패킷 메시지 등록 
        internal static void Initialize()
        {
            PacketProcessorManager.Instance.RegisterProcessor<handler_notify_socket_connected_cs2auth>();
            PacketProcessorManager.Instance.RegisterProcessor<handler_notify_socket_disconnected_cs2auth>();
        }
    }
}
