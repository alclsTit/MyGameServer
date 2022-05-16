using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Common;
using ServerEngine.Protocol;
using ServerEngine.Network.Message;

namespace AuthServer.Handler
{
    public class handler_notify_socket_connected_cs2auth : PacketProcessor
    {
        public handler_notify_socket_connected_cs2auth()
            : base((ushort)ePacketId.notify_socket_session_connected)
        {
        }

        public override void Process(ArraySegment<byte> buffer)
        {
            try
            {
                var curtick = GetTickCount();
                var notify_msg = PacketParser.Instance.BufferToMessage<Message_World.notify_socket_session_connected>(buffer);

                if (logger.IsDebugEnabled)
                    Console.WriteLine($"[{notify_msg.remoteIP}] Socket Connected[{notify_msg.id}]({GetPacketDelayTime(curtick, notify_msg.checkTime)}): Packet Transmission Success!!!");

                LogPacketDelayTime(curtick, notify_msg.checkTime);
            }
            catch (Exception ex)
            {
                OnErrorHandler(ex);
            }
        }

        public override void Clean()
        {
            base.Clean();
        }
    }


    public class handler_notify_socket_disconnected_cs2auth : PacketProcessor
    {
        public handler_notify_socket_disconnected_cs2auth()
            : base((ushort)ePacketId.notify_socket_session_disconnected)
        {

        }

        public override void Process(ArraySegment<byte> buffer)
        {
            try
            {
                var curtick = GetTickCount();
                var notify_msg = PacketParser.Instance.BufferToMessage<Message_World.notify_socket_session_disconnected>(buffer);

                Console.WriteLine($"[Data] = {notify_msg.id} - {notify_msg.size} - {notify_msg.ip} - {notify_msg.port} - {notify_msg.logdate} - {notify_msg.reason}");

                LogPacketDelayTime(curtick, notify_msg.checkTime);
            }
            catch (Exception ex)
            {
                OnErrorHandler(ex);
            }
        }

        public override void Clean()
        {
            base.Clean();
        }
    }
}
