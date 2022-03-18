using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.Message
{
    /// <summary>
    /// 등록된 패킷 메시지 자동처리 작업관리 매니저 추상 클래스
    /// 외부 프로세스에서 해당 클래스를 상속받아 단독(외부에서 싱글톤으로 구현필요)으로 사용한다
    /// </summary>
    public class PacketProcessorManager 
    {
        /// <summary>
        /// 외부 핸들러에서 실질적으로 받은 패킷을 이용하여 작업을 진행할 부분에 대한 호출 델리게이트
        /// </summary>
        /// <param name="buffer"></param>
        public delegate void ProcessFunc(ArraySegment<byte> buffer);

        /// <summary>
        /// 프로세싱 컨테이너 (key = Protocol.ePacketId / value = ProcessFunc 호출 델리게이트)
        /// </summary>
        private Dictionary<ushort, ProcessFunc> mProcessor = new Dictionary<ushort, ProcessFunc>();

        /// <summary>
        /// 외부에서 가져온 로거 세팅
        /// </summary>
        private ServerEngine.Log.Logger logger;

        /// <summary>
        /// 전역으로 하나의 객체만 사용. 외부에서 패킷메시지를 등록하여 이곳에서 전체관리
        /// </summary>
        private readonly static PacketProcessorManager mInstance = new PacketProcessorManager();
        public static PacketProcessorManager Instance => mInstance;
        private PacketProcessorManager() { }

        public bool IsExist(ushort id) => mProcessor.ContainsKey(id);

        internal void Initialize(ServerEngine.Log.Logger logger)
        {
            this.logger = logger;
        }

        private void RegisterProcessor(ushort id, ProcessFunc func)
        {
            if (IsExist(id))
                return;

            mProcessor.Add(id, func);
        }

        public void RegisterProcessor<THandler>() where THandler : IPacketProcessor, new()
        {
            var processor = new THandler();
            var id = processor.packetId;

            processor.Prepare(logger);
            ProcessFunc func = processor.Process;

            RegisterProcessor(id, func);
        }

        public bool TryRemoveItem(ushort id)
        {
            if (!IsExist(id))
                return false;

            mProcessor.Remove(id);

            return true;
        }

        internal bool ProcessBuffer(ushort id, ArraySegment<byte> buffer)
        {
            if (!IsExist(id))
                return false;

            var processor = mProcessor[id];
            processor(buffer);

            return true;
        }
    }

}
