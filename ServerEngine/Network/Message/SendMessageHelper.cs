using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerEngine.Network.Message
{
    public class SendMessageHelper
    {
        public static ThreadLocal<SendMessageBuilder> msCurrentBuffer = new ThreadLocal<SendMessageBuilder>(() => null);

        public static int msChunkSize { get; private set; }

        public static void Initialize(int bufferSize)
        {
            msChunkSize = bufferSize;
        }

        public static ArraySegment<byte> Open(int reserved)
        {
            if (msCurrentBuffer.Value == null)
                msCurrentBuffer.Value = new SendMessageBuilder(msChunkSize);

            if (msCurrentBuffer.Value.GetFreeSpace < reserved)
            {
                msCurrentBuffer.Value = null;
                msCurrentBuffer.Value = new SendMessageBuilder(msChunkSize);
            }

            return msCurrentBuffer.Value.Open(reserved);
        }

        public static ArraySegment<byte> Close(int used)
        {
            return msCurrentBuffer.Value.Close(used);
        }
    }
}
