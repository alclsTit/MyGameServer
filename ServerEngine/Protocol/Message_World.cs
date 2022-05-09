using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using ProtoBuf;

using ServerEngine.Network.Message;

namespace ServerEngine.Protocol
{
    /// <summary>
    /// 월드 전역에서 사용되는 패킷메시지 정보
    /// </summary>
    public static class Message_World
    {
        [ProtoContract]
        public sealed class notify_socket_session_connected : Packet
        {
            [ProtoMember(4)]
            public string remoteIP;

            [ProtoMember(5)]
            public string sessionID;

            public notify_socket_session_connected() : base((ushort)ePacketId.notify_socket_session_connected) { }
        }

        [ProtoContract]
        public sealed class notify_socket_session_disconnected : Packet
        {
            [ProtoMember(4)]
            public string logdate;
            [ProtoMember(5)]
            public int reason;
            [ProtoMember(6)]
            public string ip;
            [ProtoMember(7)]
            public ushort port;

            public notify_socket_session_disconnected() : base((ushort)ePacketId.notify_socket_session_disconnected) { }
        }
    }
}
