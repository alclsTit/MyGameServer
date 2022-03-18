using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Common;

namespace ServerEngine.Network.Message
{
    public abstract class PacketProcessor : IPacketProcessor
    {
        protected double MAX_PACKET_DELAY_TIME = 10;

        public ushort packetId { get; private set; }
       
        public ushort GetPacketId => packetId;

        public ServerEngine.Log.Logger logger { get; private set; }

        protected PacketProcessor(ushort id)
        {
            packetId = id;
        }

        protected long GetTickCount()
        {
            return DateTime.Now.Ticks;
        }

        private double GetPacketDelayTime(long arriveTime, long startTime)
        {
            TimeSpan diffTime = new TimeSpan(arriveTime - startTime);
            return diffTime.TotalSeconds;
        }

        public void LogPacketDelayTime(long arriveTime, long startTime)
        {
            var diffTime = GetPacketDelayTime(arriveTime, startTime);
            if (diffTime > MAX_PACKET_DELAY_TIME)
                logger.Error($"[PACKET DELAY({diffTime})] Packet delay was occured!!!");
        }

        public virtual void Prepare(ServerEngine.Log.Logger logger)
        {
            this.logger = logger;   
        }

        public abstract void Process(ArraySegment<byte> buffer);

        public virtual void Clean()
        {
            return;
        }

        protected void OnErrorHandler(Exception ex)
        {
            logger.Error(this.ClassName(), this.MethodName(), ex);
        }

    }

}
