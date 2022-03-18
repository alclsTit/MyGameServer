using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace ServerEngine.Common
{
    public static class AsyncCallbackChecker
    {
        public static bool CheckCallbackHandler(SocketError error, int byteTransferred)
        {
            if (error != SocketError.Success)
                return false;

            if (byteTransferred <= 0)
                return false;

            return true;
        }

        public static bool CheckCallbackHandler_SocketError(SocketError error)
        {
            return error == SocketError.Success;
        }

        public static bool CheckCallbackHandler_ByteTransferred(int byteTransferred)
        {
            return byteTransferred > 0;
        }
    }
}
