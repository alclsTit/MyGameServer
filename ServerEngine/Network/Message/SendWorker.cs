using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Serilog.Configuration;
using ServerEngine.Config;

namespace ServerEngine.Network.Message
{
    public class SendWorker
    {
        public readonly int MaxIOThreadCount;
        public ConcurrentDictionary<int, Channel<ArraySegment<byte>>> SendQueueConcurrentDic { get; private set; } // ConcurrentDictionary offers thread-safe index access

        public SendWorker(IConfigNetwork config_network)
        {
            var config_socket = config_network.config_socket;
            MaxIOThreadCount = config_network.max_io_thread_count;
            SendQueueConcurrentDic = new ConcurrentDictionary<int, Channel<ArraySegment<byte>>>();

            for (var i = 0; i < config_network.max_io_thread_count; ++i)
            {
                var channel = Channel.CreateBounded<ArraySegment<byte>>(capacity: config_socket.send_buff_size);
                SendQueueConcurrentDic.TryAdd(i, channel);
            }
        }

        public void Add(ArraySegment<byte> buffer, int index)
        {
            if (null == buffer.Array)
                throw new ArgumentNullException(nameof(buffer));

            if (index < 0 || index > MaxIOThreadCount)
                throw new ArgumentException(nameof(index));

            SendQueueConcurrentDic[index].Writer.TryWrite(buffer);
        }

        public ValueTask AddAsync(ArraySegment<byte> buffer, int index)
        {
            if (null == buffer.Array)
                throw new ArgumentNullException(nameof(buffer));

            if (index < 0 || index > MaxIOThreadCount)
                throw new ArgumentException(nameof(index));

            return SendQueueConcurrentDic[index].Writer.WriteAsync(buffer);
        }

        public bool Process(int index, [NotNullWhen(true)] out ArraySegment<byte>? buffer)
        {
            var result = SendQueueConcurrentDic[index].Reader.TryPeek(out var _buffer);
            if (result)
            {
                buffer = _buffer;
                return true;
            }
            else
            {
                buffer = default;
                return false;
            }
        }


    }
}
