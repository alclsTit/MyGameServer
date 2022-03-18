using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.Message
{
    public class SendMessageBuilder
    {
        private byte[] mSendBuffer;

        private int mUsedSpace;

        public int GetFreeSpace => mSendBuffer.Length - mUsedSpace;

        public SendMessageBuilder(int chunk)
        {
            mSendBuffer = new byte[chunk];
        }

        public ArraySegment<byte> Open(int reserved)
        {
            if (reserved > GetFreeSpace)
                return default(ArraySegment<byte>);

            return new ArraySegment<byte>(mSendBuffer, mUsedSpace, reserved);
        }

        public ArraySegment<byte> Close(int used)
        {
            var oldBuffer = new ArraySegment<byte>(mSendBuffer, mUsedSpace, used);
            mUsedSpace += used;
            return oldBuffer;
        }
    }
}
