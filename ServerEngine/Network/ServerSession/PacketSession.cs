using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ServerEngine.Common;
using ServerEngine.Network.Message;
using System.Net;

namespace ServerEngine.Network.ServerSession
{
    // 24.09.09 삭제 예정
    /*public abstract class PacketSession : Session
    {
        // 패킷 구조 =                헤더                       +       바디 
        //            [패킷 바디사이즈 + 메시지 아이디 + 송신시간] + [실제 패킷 메시지]
        // 서버에서 수신한 수신버퍼를 읽어 패킷 처리 진행
        public sealed override int OnReceive(ArraySegment<byte> buffer)
        {
            int processLength = 0;

            while (true)
            {            
                // 헤더 사이즈만큼도 패킷 데이터를 읽지 못했을 때
                if (buffer.Count < PacketHeaderInfo.MAX_PACKET_HEADER_SIZE)
                    break;

                // 헤더 사이즈 읽기
                var headerSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (headerSize > buffer.Count)
                {
                    break;
                }
                else 
                {
                    // 1. 수신된 데이터가 수신버퍼 사이즈를 초과하였는지 체크
                    // 2. 수신된 패킷 사이즈가 SocketAsyncEventArgs 수신버퍼로 받은 바이트 수보다 큰지 체크
                    if (CheckRecvPacketValidate(buffer))
                    {
                        var offset = buffer.Offset;
                        var length = headerSize;
                        Task.Run(() => ProcessReceivePacket(new ArraySegment<byte>(buffer.Array, offset, length)));
                    }
                }

                processLength += headerSize;
                buffer = new ArraySegment<byte>(buffer.Array, buffer.Offset + headerSize, buffer.Count - headerSize);
            }

            return processLength;
        }

        // Recv 된 패킷 중 조립이 완료된 대상에 한하여 작업 진행 
        // buffer 값을 패킷으로 만들고 등록해둔 패킷 id에 맞는 작업을 진행하는 패킷 처리 프로세싱 진행
        // 해당 로직은 ThreadSafe 해야됨
        public void ProcessReceivePacket(ArraySegment<byte> buffer)
        {
            // 온전한 패킷이 들어왔으므로 처리 
            // 1. 버퍼에 담긴 패킷 아이디 및 사이즈를 찾는다 
            var id = BitConverter.ToUInt16(buffer.Array, PacketHeaderInfo.MAX_PACKET_HEADER_SIZE);

            // 2. 패킷아이디와 버퍼정보를 넘겨줘서 외부에서 process 패킷처리 진행하도록 한다
            if (!PacketProcessorManager.Instance.ProcessBuffer(id, buffer))
                logger.Error(this.ClassName(), this.MethodName(), $"Message id[{id}] isn't existed in PacketProcessorManager!!!");
        }

        protected override bool CheckRecvPacketValidate(ArraySegment<byte> buffer)
        {
            if (buffer.Count > mServerInfo.recvBufferSize)
            {
                logger.Error(this.ClassName(), this.MethodName(), "RecvBuffer is bigger than reserved size");
                return false;
            }

            var size = BitConverter.ToUInt16(buffer.Array, 0);
            if (buffer.Count < size)
            {
                logger.Error(this.ClassName(), this.MethodName(), "Size Of PacketHeader is bigger than RecvBuffer");
                return false;
            }

            return true;
        }

        protected override bool CheckSendPacketValidate(ArraySegment<byte> buffer, ushort size)
        {
            if (buffer.Count > mServerInfo.sendBufferSize)
                return false;

            if (buffer.Count < size)
                return false;

            return true;
        }

        public sealed override void OnSend(int numOfBytes)
        {

        }
    }
    */
}
