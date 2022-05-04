using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.Message
{
    /// <summary>
    /// Recv 수신버퍼
    /// * Session 당 한개씩 배정
    /// </summary>
    public class MessageProcessor
    {
        private ArraySegment<byte> mRecvBuffer;
        private int mNumOfWrite;
        private int mNumOfRead;

        /// <summary>
        /// 읽어야되는 데이터 크기
        /// </summary>
        public int GetDataSize => mNumOfWrite - mNumOfRead;

        /// <summary>
        /// 메시지를 쓸 수 있는 잔여크기
        /// </summary>
        public int GetLeftSize => mRecvBuffer.Count - mNumOfWrite;

        /// <summary>
        /// 현재 데이터의 유효범위 (유효범위 = 전체 쓰여진 데이터 - 현재까지 읽은 데이터)
        /// </summary>
        public ArraySegment<byte> GetReadMessage => new ArraySegment<byte>(mRecvBuffer.Array, mRecvBuffer.Offset + mNumOfRead, GetDataSize);

        /// <summary>
        /// 메시지를 쓸 수 있는 잔여공간
        /// </summary>
        public ArraySegment<byte> GetWriteMessage => new ArraySegment<byte>(mRecvBuffer.Array, mRecvBuffer.Offset + mNumOfWrite, GetLeftSize);

        //[*][*][*][*][*][]
        //->[][][][][]
        public MessageProcessor(int bufferSize)
        {
            mRecvBuffer = new ArraySegment<byte>(new byte[bufferSize]);
        }

        public bool OnReadMessage(int numOfBytes)
        {
            if (numOfBytes > GetDataSize)
                return false;

            mNumOfRead += numOfBytes;
            return true;
        }

        public bool OnWriteMessage(int numOfBytes)
        {
            if (numOfBytes > GetLeftSize)
                return false;

            mNumOfWrite += numOfBytes;
            return true;
        }

        public void Clear()
        {
            var dataSize = GetDataSize;
            if (dataSize == 0)
            {
                // 남은 데이터가 없으면 복사하지 않고 커서 위치만 리셋
                mNumOfRead = mNumOfWrite = 0;
            }
            else
            {
                // 남은 데이터가 있으면 시작 위치로 복사
                Array.Copy(mRecvBuffer.Array, mRecvBuffer.Offset + mNumOfRead, mRecvBuffer.Array, mRecvBuffer.Offset, dataSize);
                mNumOfWrite = dataSize;
                mNumOfRead = 0;
            }
        }
    }
}
