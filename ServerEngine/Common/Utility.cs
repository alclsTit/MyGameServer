using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Common
{
    public interface IAsyncEventCallbackHandler
    {
        public delegate void AsyncEventCallbackHandler(object? sender, SocketAsyncEventArgs e);
    }

    class Utility
    {
    }
}
