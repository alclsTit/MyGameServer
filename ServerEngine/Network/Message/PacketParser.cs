using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FlatSharp; 

namespace ServerEngine.Network.Message
{
    public class PacketParser
    {
        /// <summary>
        /// * 싱글턴 클래스는 하나의 인스턴스만을 갖는다. 매개변수로 사용할 수 있다. 상속계층을 갖을 수 있다
        /// * 정적 클래스는 인스턴스를 갖지않는다.매개변수로 사용할 수 없다.상속 할 수 없다. 하나의 정적 생성자를 갖을 수 있으나 초기화 시점을 지정할 수 없다
        /// 실제 사용될 때 인스턴스가 생성되는 게으른생성을 통한 싱글톤 패턴사용
        /// </summary>
        private static readonly Lazy<PacketParser> mInstance = new Lazy<PacketParser>(() => new PacketParser());
        public static PacketParser Instance => mInstance.Value;

        private PacketParser() { }

        #region "Serialize"
        /// <summary>
        /// 패킷 시리얼라이징 (Packet -> Buffer / 메시지를 주는 쪽에서 처리)
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="packet"></param>
        /// <returns></returns>
        public ArraySegment<byte> MessageToBuffer<TPacket>(TPacket packet) where TPacket : Packet
        {
            // ArraySegment 내부의 array가 null, offset = count = 0 인 상태로 반환
            if (packet == null)
                return default(ArraySegment<byte>);

            using (var stream = new MemoryStream())
            {
                // 패킷 시리얼라이징
                ProtoBuf.Serializer.Serialize(stream, packet);

                // 바이트배열에 패킷시리얼라이징을 위해서 패킷 전체크기(헤더 + 바디) 저장
                var sizeOfBody = (ushort)stream.Length;
                packet.SetSize(sizeOfBody, true);
       
                int offset = 0;
                Span<byte> spanBuffer = new Span<byte>(new byte[packet.size]);  //buffer = SendMessageHelper.Open(packet.size);
                if (SerializeHeader(ref spanBuffer, ref offset, packet.id, packet.size))
                {
                    // 바디 메시지 -> 결과 패킷 배열로 이동
                    var resultBuffer = spanBuffer.ToArray();
                    Buffer.BlockCopy(stream.ToArray(), 0, resultBuffer, offset, sizeOfBody);
                    return resultBuffer;
                }
                else
                {
                    return default(ArraySegment<byte>);
                }
            }
        }

        /// <summary>
        /// 패킷헤더 시리얼라이징
        /// </summary>
        /// <param name="spanBuffer"></param>
        /// <param name="offset"></param>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool SerializeHeader(ref Span<byte> spanBuffer, ref int offset, ushort id, ushort size)
        {
            var sizeOfHeaderSize = PacketHeaderInfo.MAX_PACKET_HEADER_SIZE;
            var sizeOfHeaderId = PacketHeaderInfo.MAX_PACKET_HEADER_ID;
            var sizeOfHeaderCheckTime = PacketHeaderInfo.MAX_PACKET_HEADER_TICKCOUNT;

            bool result = true;

            result &= BitConverter.TryWriteBytes(spanBuffer.Slice(offset, sizeOfHeaderSize), size);
            offset += sizeOfHeaderSize;

            result &= BitConverter.TryWriteBytes(spanBuffer.Slice(offset, sizeOfHeaderId), id);
            offset += sizeOfHeaderId;

            result &= BitConverter.TryWriteBytes(spanBuffer.Slice(offset, sizeOfHeaderCheckTime), DateTime.Now.Ticks);
            offset += sizeOfHeaderCheckTime;

            return result;
        }
        #endregion

        #region "Deserialize"
        /// <summary>
        /// 패킷 디시리얼라이징 (Buffer -> Packet / 메시지 받은 쪽에서 처리) 
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="buffer"></param>
        /// <param name="size">수신된 Recv 버퍼사이즈</param>
        /// <returns></returns>
        public TPacket BufferToMessage<TPacket>(ArraySegment<byte> buffer) where TPacket : Packet
        {
            // ArraySegment 내부의 array가 null인 상태로 넘어올 경우 null로 반환
            if (buffer.Array == null)
                return null;

            TPacket packet;
            ReadOnlySpan<byte> spanBuffer = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);

            // 헤더 정보 
            int offset = 0;
            var sizeOfHeader = PacketHeaderInfo.MAX_PACKET_HEADER_SIZE + PacketHeaderInfo.MAX_PACKET_HEADER_ID + PacketHeaderInfo.MAX_PACKET_HEADER_TICKCOUNT;
            var sizeOfPacket = BitConverter.ToUInt16(spanBuffer.Slice(offset, PacketHeaderInfo.MAX_PACKET_HEADER_SIZE));
            var sizeOfBody = sizeOfPacket - sizeOfHeader;

            // 바디 디시리얼라이징
            var newBuffer = new ArraySegment<byte>(new byte[sizeOfBody]);
            Buffer.BlockCopy(buffer.Array, sizeOfHeader, newBuffer.Array, 0, sizeOfBody);
            using (var stream = new MemoryStream(newBuffer.Array))
            {
                packet = ProtoBuf.Serializer.Deserialize<TPacket>(stream);
                DeserializeHeader(ref spanBuffer, ref packet, sizeOfPacket, offset);
                return packet;
            }
        }

        /// <summary>
        /// 패킷헤더 디시리얼라이징
        /// </summary>
        /// <typeparam name="TPacket"></typeparam>
        /// <param name="spanBuffer"></param>
        /// <param name="packet"></param>
        /// <param name="sizeOfPacket"></param>
        /// <param name="offset"></param>
        private void DeserializeHeader<TPacket>(ref ReadOnlySpan<byte> spanBuffer, ref TPacket packet, ushort sizeOfPacket, int offset) where TPacket : Packet
        {
            packet.SetSize(sizeOfPacket, false);
            offset += PacketHeaderInfo.MAX_PACKET_HEADER_SIZE;

            packet.SetID(BitConverter.ToUInt16(spanBuffer.Slice(offset, PacketHeaderInfo.MAX_PACKET_HEADER_ID)));
            offset += PacketHeaderInfo.MAX_PACKET_HEADER_ID;
            
            packet.SetCheckTime(BitConverter.ToInt64(spanBuffer.Slice(offset, PacketHeaderInfo.MAX_PACKET_HEADER_TICKCOUNT)));
        }
        #endregion

        #region "폐기된 메서드"
        /* .Net Framework - 폐기된 메서드
         * public ArraySegment<byte> MessageToBuffer<TPacket>(TPacket packet) where TPacket : Packet
        {
            // ArraySegment 내부의 array가 null, offset = count = 0 인 상태로 반환
            if (packet == null)
                return default(ArraySegment<byte>);

            int offset = 0;

            ArraySegment<byte> buffer;
            using (var stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(stream, packet);
                packet.SetSize(stream.Length, true);

                //buffer = SendMessageHelper.Open(packet.size);
                buffer = new ArraySegment<byte>(new byte[packet.size]);

                AddHeaderToMessageInSerialize(ref buffer, ref offset, packet.id, packet.size);
                Array.Copy(stream.ToArray(), 0, buffer.Array, offset, stream.Length);
            }

            return buffer;
        }
        */

        /* .Net Framework - 폐기된 메서드
        private void AddHeaderToMessageInSerialize(ref ArraySegment<byte> buffer, ref int offset, ushort id, ushort size) 
        {
            var sizeOfHeaderSize = PacketHeaderInfo.MAX_PACKET_HEADER_SIZE;
            var sizeOfHeaderId = PacketHeaderInfo.MAX_PACKET_HEADER_ID;
            var sizeOfHeaderCheckTime = PacketHeaderInfo.MAX_PACKET_HEADER_TICKCOUNT;

            Buffer.BlockCopy(BitConverter.GetBytes(size), 0, buffer.Array, offset, sizeOfHeaderSize);
            offset += sizeOfHeaderSize;

            Buffer.BlockCopy(BitConverter.GetBytes(id), 0, buffer.Array, offset, sizeOfHeaderId);
            offset += sizeOfHeaderId;

            Buffer.BlockCopy(BitConverter.GetBytes(DateTime.Now.Ticks), 0, buffer.Array, offset, sizeOfHeaderCheckTime);
            offset += sizeOfHeaderCheckTime;
        }
        */


        /* .Net Framework - 폐기된 메서드
         * private void AddHeaderToMessageInDeserialize<TPacket>(ref ArraySegment<byte> buffer, TPacket packet) where TPacket : Packet
        {
            var offset = 0;
            packet.SetSize(BitConverter.ToUInt16(buffer.Array, offset), false);

            offset += PacketHeaderInfo.MAX_PACKET_HEADER_SIZE;
            packet.SetId(BitConverter.ToUInt16(buffer.Array, offset));

            offset += PacketHeaderInfo.MAX_PACKET_HEADER_ID;
            packet.SetCheckTime(BitConverter.ToInt64(buffer.Array, offset));
        }*/
        #endregion
    }
}
