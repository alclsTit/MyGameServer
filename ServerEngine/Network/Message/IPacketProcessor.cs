using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.Message
{
    public interface IPacketProcessor
    {
        ushort packetId { get; }

        ServerEngine.Log.Logger logger { get; }

        void Prepare(ServerEngine.Log.Logger logger);

        void Process(ArraySegment<byte> buffer);

        void Clean();
    }
}
