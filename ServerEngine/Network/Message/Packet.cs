using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using ProtoBuf;

namespace ServerEngine.Network.Message
{
    [ProtoContract]
    public abstract class Packet
    {
        // [헤더] 1. 메시지 아이디
        [ProtoMember(1)]
        public ushort id { get; protected set; }
        // [헤더] 2. 패킷 사이즈 - 패킷 사이즈는 id와 size 크기를 제외한 메시지 바디 크기
        [ProtoMember(2)]
        public ushort size { get; protected set; }
        // [헤더] 3. 패킷 딜레이 체크를 위한 패킷 송신 전 시간
        [ProtoMember(3)]
        public Int64 checkTime { get; protected set; }

        protected Packet(ushort id)
        {
            this.id = id;   
        }

        public void SetID(ushort id)
        {
            this.id = id;
        }

        public void SetSize(ushort sizeOfBody, bool serialize)
        {
            if (serialize)
            {
                var sizeOfHeader = sizeof(ushort) * 2 + sizeof(Int64);
                var totalSize = sizeOfHeader + sizeOfBody;
                size = (ushort)totalSize;
            }
            else
            {
                size = (ushort)sizeOfBody;
            }
        }

        public void SetCheckTime(Int64 time)
        {
            checkTime = time;
        }
    }

}
