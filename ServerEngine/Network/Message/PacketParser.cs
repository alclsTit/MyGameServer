using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NetworkEngineMessage;
using Google.Protobuf;
using ServerEngine.Common;
using System.Configuration;

namespace ServerEngine.Network.Message
{
    // 서버에서 사용하는 MessageParser 전역처리 
    //  - Deserialize 진행시 생성 및 파괴를 통한 과도한 GC 발생되는 것을 방지
    //  - 메모리 효율성
    public static class ProtoMessageParser
    {
        private enum eMessageType
        {
            Client = 0,
            Server = 1
        }

        private static bool m_initialized = false;
        private static readonly int m_client_index = (int)eMessageType.Client;
        private static readonly int m_server_index = (int)eMessageType.Server;

        // 0: client message
        // 1: server message
        private static readonly List<Dictionary<ushort, MessageParser>> m_parsers = new List<Dictionary<ushort, MessageParser>>()
        {
            new Dictionary<ushort, MessageParser>(),    // client parser
            new Dictionary<ushort, MessageParser>()     // server parser
        };

        public static MessageParser? GetClientParser(ushort message_id) => GetParser(m_client_index, message_id);
        public static MessageParser? GetServerParser(ushort message_id) => GetParser(m_server_index, message_id);

        public static void InitializeClientMessageParsers()
        {
            //AddClientMessageParser(message_id: message_id.CpePingPongCs);
            //AddClientMessageParser(message_id: message_id.CpePingPongSc);
        }

        public static void InitializeServerMessageParsers()
        {
            AddServerMessageParser(message_id: message_id.SpePingPongCs);
            AddServerMessageParser(message_id: message_id.SpePingPongSc);
        }

        public static void AddClientMessageParser<TMessage>(ushort message_id)
            where TMessage : IMessage<TMessage>, new()
        {
            m_parsers[m_client_index].Add(message_id, new MessageParser<TMessage>(() => new TMessage()));
        }

        public static void AddServerMessageParser<TMessage>(ushort message_id)
            where TMessage : IMessage<TMessage>, new()
        {
            m_parsers[m_server_index].Add(message_id, new MessageParser<TMessage>(() => new TMessage()));
        }

        private static MessageParser? GetParser(int index, ushort message_id)
        {
            return m_parsers[index].TryGetValue(message_id, out var parser) ? parser : null;
        }

        public static void SetInitializeFlag(bool flag)
        {
            m_initialized = flag;
        }

        public static bool GetInitializeFlag()
        {
            return m_initialized;
        }
    }

    // Protobuf 관련 직렬화 / 역직렬화 
    public class ProtoParser
    {
        // serialize protobuf message 
        //  - 내부에서 버퍼 생성 및 해당 버퍼에 메시지 직렬화
        public ArraySegment<byte> Serialize<TMessage>(TMessage message, ushort message_id) where TMessage : IMessage
        {
            if (null == message)
                throw new ArgumentNullException(nameof(message));

            if (message_id > Utility.MAX_PACKET_DEFINITION_SIZE)
                throw new ArgumentOutOfRangeException(nameof(message_id));

            var header_size = Utility.MAX_PACKET_HEADER_SIZE + Utility.MAX_PACKET_HEADER_TYPE;
            var body_size = message.CalculateSize();
            var packet_size = header_size + body_size;
            var buffer = new ArraySegment<byte>(new byte[packet_size]);
            int offset = 0;

            if (null == buffer.Array)
                throw new NullReferenceException(nameof(buffer.Array));

            Buffer.BlockCopy(BitConverter.GetBytes(header_size), 0, buffer.Array, offset, Utility.MAX_PACKET_HEADER_SIZE);
            offset += Utility.MAX_PACKET_HEADER_SIZE;

            Buffer.BlockCopy(BitConverter.GetBytes(message_id), 0, buffer.Array, offset, Utility.MAX_PACKET_HEADER_TYPE);
            offset += Utility.MAX_PACKET_HEADER_TYPE;

            Buffer.BlockCopy(message.ToByteArray(), 0, buffer.Array, offset, body_size);

            return buffer;
        }

        // serialize protobuf message
        //  - 전달받은 Stream 객체의 버퍼에 메시지 직렬화
        public ArraySegment<byte> Serialize<TMessage>(TMessage message, ushort message_id, SendStream stream) where TMessage : IMessage
        {

            if (null == message) 
                throw new ArgumentNullException(nameof(message));

            if (message_id < 0 || message_id > Utility.MAX_PACKET_DEFINITION_SIZE) 
                throw new ArgumentOutOfRangeException(nameof(message_id));

            if (null == stream.Buffer.Array)
                throw new ArgumentNullException(nameof(stream.Buffer.Array));

            var header_size = Utility.MAX_PACKET_HEADER_SIZE + Utility.MAX_PACKET_HEADER_TYPE;
            var body_size = message.CalculateSize();
            var packet_size = header_size + body_size;
            int offset = 0;

            if (stream.Buffer.Array.Length < packet_size)
                throw new InvalidOperationException("SendStream.Buffer is too small for the serialized packet");

            Buffer.BlockCopy(BitConverter.GetBytes(header_size), 0, stream.Buffer.Array, offset, Utility.MAX_PACKET_HEADER_SIZE);
            offset += Utility.MAX_PACKET_HEADER_SIZE;

            Buffer.BlockCopy(BitConverter.GetBytes(message_id), 0, stream.Buffer.Array, offset, Utility.MAX_PACKET_HEADER_TYPE);
            offset += Utility.MAX_PACKET_HEADER_TYPE;

            Buffer.BlockCopy(message.ToByteArray(), 0, stream.Buffer.Array, offset, body_size);

            return stream.Buffer;
        }

        // check and set. serialize protobuf message.
        //  - 전달받은 Stream 객체의 버퍼에 메시지 직렬화 및 결과 반환
        public bool TrySerialize<TMessage>(TMessage message, ushort message_id, SendStream stream, int offset = 0) where TMessage: IMessage
        {
            if (null == message)
                throw new ArgumentNullException(nameof(message));

            if (message_id < 0 || message_id > Utility.MAX_PACKET_DEFINITION_SIZE)
                throw new ArgumentOutOfRangeException(nameof(message_id));

            if (null == stream.Buffer.Array)
                throw new ArgumentNullException(nameof(stream.Buffer.Array));

            var header_size = Utility.MAX_PACKET_HEADER_SIZE + Utility.MAX_PACKET_HEADER_TYPE;
            var body_size = message.CalculateSize();
            var packet_size = header_size + body_size;

            if (stream.Buffer.Array.Length < packet_size)
                throw new InvalidOperationException("SendStream.Buffer is too small for the serialized packet");

            Buffer.BlockCopy(BitConverter.GetBytes(header_size), 0, stream.Buffer.Array, offset, Utility.MAX_PACKET_HEADER_SIZE);
            offset += Utility.MAX_PACKET_HEADER_SIZE;

            Buffer.BlockCopy(BitConverter.GetBytes(message_id), 0, stream.Buffer.Array, offset, Utility.MAX_PACKET_HEADER_TYPE);
            offset += Utility.MAX_PACKET_HEADER_TYPE;

            Buffer.BlockCopy(message.ToByteArray(), 0, stream.Buffer.Array, offset, body_size);

            return true;
        }

        // proto 파일 컨버팅시 생성되는 message는 기본생성자를 포함하고 있다 > new() 제약조건 가능 
        // deserialize protobuf message
        //  - 전달받은 버퍼에 담긴 데이터를 메시지로 역직렬화
        public TMessage Deserialize<TMessage>(bool client_message, ref ArraySegment<byte> buffer) 
            where TMessage : IMessage<TMessage>, new()
        {
            if (null == buffer.Array)
                throw new ArgumentNullException(nameof(buffer));

            int offset = 0;
            var read_span = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);

            var packet_size = BitConverter.ToUInt16(read_span.Slice(offset, Utility.MAX_PACKET_HEADER_SIZE));
            offset += Utility.MAX_PACKET_HEADER_SIZE;

            var message_id = BitConverter.ToUInt16(read_span.Slice(offset, Utility.MAX_PACKET_HEADER_TYPE));
            offset += Utility.MAX_PACKET_HEADER_TYPE;

            var header_size = Utility.MAX_PACKET_HEADER_SIZE + Utility.MAX_PACKET_HEADER_TYPE;
            var body_size = packet_size - header_size;

            if (body_size <= 0)
                throw new InvalidOperationException($"Invalid packet size. size = {packet_size}, header = {header_size},7uk body = {body_size}");

            //var parser = new MessageParser<TMessage>(() => new TMessage());
            //return parser.ParseFrom(buffer.Array, header_size, body_size);
            var parser = client_message
                         ? ProtoMessageParser.GetClientParser(message_id)
                         : ProtoMessageParser.GetServerParser(message_id);

            if (parser == null)
                throw new InvalidOperationException($"Invalid packet type. Message ID: {message_id}, Client: {(client_message ? "True" : "False")}");

            return (TMessage)parser.ParseFrom(buffer.Array, header_size, body_size);
        }
    }

    // Packet 객체를 상속한 대상에 대한 직렬화 / 역직렬화
    // Todo: Protobuf 이외의 패킷에 대한 직렬화 / 역직렬화 기능을 갖춘 클래스로 구현 예정
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

            // Span 버퍼의 offset에서 시작하여 sizeOfHeaderSize 만큼의 공간에 size 값을 쓴다
            result &= BitConverter.TryWriteBytes(spanBuffer.Slice(offset, sizeOfHeaderSize), size);
            offset += sizeOfHeaderSize;

            // Span 버퍼의 offset에서 시작하여 sizeOfHeaderId 만큼의 공간에 id 값을 쓴다
            result &= BitConverter.TryWriteBytes(spanBuffer.Slice(offset, sizeOfHeaderId), id);
            offset += sizeOfHeaderId;

            // Span 버퍼의 offset에서 시작하여 sizeOfHeaderCheckTime 만큼의 공간에 DateTime.Now.Ticks 값을 쓴다
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
            // 전달받은 buffer의 데이터를 읽기만 할 것이므로 읽기전용버퍼인 ReadOnlySpan 사용
            ReadOnlySpan<byte> spanBuffer = new ReadOnlySpan<byte>(buffer.Array, buffer.Offset, buffer.Count);

            // 헤더 사이즈, 바디 사이즈 세팅
            int offset = 0;
            var sizeOfHeader = PacketHeaderInfo.MAX_PACKET_HEADER_SIZE + PacketHeaderInfo.MAX_PACKET_HEADER_ID + PacketHeaderInfo.MAX_PACKET_HEADER_TICKCOUNT;
            var sizeOfPacket = BitConverter.ToUInt16(spanBuffer.Slice(offset, PacketHeaderInfo.MAX_PACKET_HEADER_SIZE));
            var sizeOfBody = sizeOfPacket - sizeOfHeader;

            // 패킷 바디에 대한 역직렬화 작업 진행
            var newBuffer = new ArraySegment<byte>(new byte[sizeOfBody]);
            Buffer.BlockCopy(buffer.Array, sizeOfHeader, newBuffer.Array, 0, sizeOfBody);
            using (var stream = new MemoryStream(newBuffer.Array))
            {
                // 패킷 바디 역직렬화를 통해 패킷 메시지 내용 추출 
                packet = ProtoBuf.Serializer.Deserialize<TPacket>(stream);
                // 패킷 헤더 내용 구성 
                DeserializeHeader(ref spanBuffer, packet, sizeOfPacket, offset);
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
        private void DeserializeHeader<TPacket>(ref ReadOnlySpan<byte> spanBuffer, TPacket packet, ushort sizeOfPacket, int offset) where TPacket : Packet
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
