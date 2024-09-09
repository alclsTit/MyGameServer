using log4net;
using log4net.Repository.Hierarchy;
using ServerEngine.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ServerEngine.Network.Message
{
    public class RecvMessageHandler
    {
        private Log.ILogger Logger;
        //private ArraySegment<byte> mBuffer;
        private RecvStream mRecvStream;

        public int MaxBufferSize { get; private set; }
        private int mNumOfWrite = 0;
        private int mNumOfRead = 0;

        public RecvMessageHandler(int max_buffer_size, Log.ILogger logger)
        {
            this.Logger = logger;

            MaxBufferSize = max_buffer_size;
            //mBuffer = new ArraySegment<byte>(new byte[max_buffer_size]);
            mRecvStream = new RecvStream(max_buffer_size);

            // [] [] [] [] [] [] [] []... [] => mBuffer.Array (origin)
            // s           e              => mBuffer (used 0 ~ 3)
            // s              e           => mBuffer (used 0 ~ 4)

        }

        public RecvMessageHandler(RecvStream stream, int max_buffer_size, Log.ILogger logger)
        {
            this.Logger = logger;
            MaxBufferSize = max_buffer_size;

            mRecvStream = stream;
        }

        #region property
        /// <summary>
        /// Get RecvBuffer
        /// </summary>
        public ArraySegment<byte> GetBuffer => mRecvStream.Buffer;

        /// <summary>
        /// buffer leftsize. The space that we can write
        /// </summary>
        /// <returns></returns>
        public int GetLeftSize
        {
            //get { return mBuffer.Count - mNumOfWrite; }
            get { return mRecvStream.Buffer.Count - mNumOfWrite; }
        }

        /// <summary>
        /// The space that we will have to read 
        /// </summary>
        /// <returns></returns>
        public int GetHaveToReadSize
        {
            get { return mNumOfWrite - mNumOfRead; }
        }
        #endregion

        #region public_method
        public bool WriteMessage(int write_bytes)
        {
            if (write_bytes > GetLeftSize)
                return false;

            mNumOfWrite += write_bytes;
            return true;
        }

        public bool ReadMessage(int read_bytes) 
        {
            if (read_bytes > GetHaveToReadSize)
                return false;

            mNumOfRead += read_bytes;
            return true;
        }

        public bool TryGetReadBuffer([NotNullWhen(true)]out ArraySegment<byte> buffer)
        {
            if (null == GetBuffer.Array)
            {
                buffer = default;
                return false;
            }

            // 기존에 메모리에 할당되어있던 array에 대한 view(참조)
            // 구조체이기 때문에 GC 대상이 아니다
            buffer = new ArraySegment<byte>(GetBuffer.Array, GetBuffer.Offset + mNumOfRead, GetHaveToReadSize);
            return true;
        }

        public bool TryGetWriteBuffer([NotNullWhen(true)] out ArraySegment<byte>? buffer)
        {
            if (null == GetBuffer.Array)
            {
                buffer = default;
                return false;
            }

            buffer = new ArraySegment<byte>(GetBuffer.Array, GetBuffer.Offset + mNumOfWrite, GetLeftSize);
            return true;
        }

        public int ProcessReceive(in ArraySegment<byte> buffer)
        {
            int process_length = 0;

            while (true)
            {
                if (null == buffer.Array)
                    break;

                if (buffer.Count < Utility.MAX_PACKET_HEADER_SIZE)
                    break;

                var header_size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                if (header_size <= buffer.Count)
                {
                    if (CheckValidate(buffer, header_size))
                    {
                        var offset = buffer.Offset;
                        var length = header_size;

                        var header_id = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                        OnProcessReceiveHandler(buffer, header_id);
                    }
                }  
                else
                {
                    break;
                }

                process_length += header_size;
            }

            return process_length;
        }

        public bool OnProcessReceiveHandler(in ArraySegment<byte> buffer, int? nullable_header_id = default)
        {
            if (null == buffer.Array)
            {
                Logger.Error($"Error in RecvMessageHandler.OnProcessReceiveHandler() - packet buffer is null");
                return false;
            }

            int id = nullable_header_id.HasValue ? nullable_header_id.Value : BitConverter.ToUInt16(buffer.Array, Utility.MAX_PACKET_HEADER_SIZE);

            // Todo: 전달받은 packet type(id)에 맞는 등록된 패킷 핸들러를 실행하는 로직 진행
            // ArraySegment<byte>를 전달한뒤 deserialize 진행

            return true;
        }

        public bool CheckValidate(in ArraySegment<byte> buffer, int? nullable_header_size = default)
        {
            if (null == buffer.Array)
            {
                Logger.Error($"Error in RecvMessageHandler.CheckValidate() - packet buffer is null");
                return false;
            }

            if (buffer.Count > MaxBufferSize)
            {
                Logger.Error($"Error in RecvMessageHandler.CheckValidate() - packet read size is bigger than maximum size of buffer");
                return false;
            }
;
            int header_size = nullable_header_size.HasValue? nullable_header_size.Value : BitConverter.ToUInt16(buffer.Array, 0);
            if (header_size > MaxBufferSize)
            {
                Logger.Error($"Error in RecvMessageHandler.CheckValidate() - size of packet is bigger than maximum size of buffer");
                return false;
            }

            return true;
        }

        public void ResetBuffer(RecvStream stream, int max_buffer_size)
        {
            mRecvStream.Reset();

            mRecvStream = stream;
            MaxBufferSize = max_buffer_size;
        }

        #endregion
    }
}
