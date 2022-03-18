using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Network.SystemLib;

namespace ServerEngine.Config
{
    public abstract class ServerConfig : IServerInfo
    {
        /// <summary>
        /// 만약, config 파일에 내용을 기재하지 않았을 경우를 대비하여 default 값을 미리 만들어둔다
        /// </summary>
        #region "Default Value"
        public readonly int DefaultMaxConnectNumber = 3000;

        public readonly int DefaultKeepAliveTime = 60 * 10; // sec

        public readonly int DefaultKeepAliveInterval = 60; // sec

        // Nagle 알고리즘은 기본적으로 사용하지 않는다 (성능이슈)
        public readonly bool DefaultNodelay = true;

        public readonly eProtocolType DefaultProtocolType = eProtocolType.TCP;

        public readonly int DefaultSendBufferSize = 1024 * 4;

        public readonly int DefaultRecvBufferSize = 1024 * 4;

        // Socket Close 된 상태에서 남은 패킷을 보내기 위해 잠시동안 연결되어있는 옵션 사용 (true / false)
        public readonly bool DefaultSocketLingerFlag = false;

        // Linger 옵션 대기 시간
        public readonly int DefaultSocketLingerDelayTime = 10;

        // Per SendingQueue 사이즈
        public readonly int DefaultSendingQueueSize = 5;

        // Default ThreadPool count control flag
        public readonly bool DefaultControlThreadPoolCount = false;

        // Default worker threadpool mincount
        public readonly int DefaultMinWorkThreadCount = Environment.ProcessorCount;

        // Default worker threadpool maxcount
        public readonly int DefaultMaxWorkThreadCount = 2000;

        // Default IO threadpool mincount
        public readonly int DefaultMinIOThreadCount = Environment.ProcessorCount;

        // Default IO threadpool maxcount
        public readonly int DefaultMaxIOThreadCount = 1000;

        // Default Encoding style (UTF-8)
        public readonly string DefaultEncoding = "UTF-8";
        #endregion

        public int maxConnectNumber { get; set; }

        public int keepAliveTime { get; set; }

        public int keepAliveInterval { get; set; }

        public bool nodelay { get; set; }

        public eProtocolType protocolType { get; set; }

        public int sendBufferSize { get; set; }

        public int recvBufferSize { get; set; }

        public bool socketLingerFlag { get; set; }

        public int socketLingerDelayTime { get; set; }

        // Per SendingQueue 사이즈 
        public int sendingQueueSize { get; set; }

        // ThreadPool count control flag
        public bool controlThreadPoolCount { get; set; }

        // Min ThreadPool(work) Count 
        public int minWorkThreadCount { get; set; }

        // Max ThreadPool(work) Count 
        public int maxWorkThreadCount { get; set; }

        // Min ThreadPool(IO) Count 
        public int minIOThreadCount { get; set; }

        // Max ThreadPool(IO) Count 
        public int maxIOThreadCount { get; set; }

        // 문자 인코딩 스타일
        public string encoding { get; set; }

        protected ServerConfig()
        {
            maxConnectNumber = DefaultMaxConnectNumber;
            keepAliveTime = DefaultKeepAliveTime;
            keepAliveInterval = DefaultKeepAliveInterval;
            nodelay = DefaultNodelay;
            protocolType = DefaultProtocolType;
            sendBufferSize = DefaultSendBufferSize;
            recvBufferSize = DefaultRecvBufferSize;
            socketLingerFlag = DefaultSocketLingerFlag;
            socketLingerDelayTime = DefaultSocketLingerDelayTime;
            sendingQueueSize = DefaultSendingQueueSize;
            minWorkThreadCount = DefaultMinWorkThreadCount; 
            maxWorkThreadCount = DefaultMaxWorkThreadCount;
            minIOThreadCount = DefaultMinIOThreadCount;
            maxIOThreadCount = DefaultMaxIOThreadCount;
            encoding = DefaultEncoding;
            controlThreadPoolCount = DefaultControlThreadPoolCount;
        }
    }

    #region "삭제예정 백업본"
    /*public class ServerConfig : IServerInfo
    {
        // Set Default Value
        //-------------------------------------------------------------------------

        /// <summary>
        /// Default max connect count
        /// </summary>
        public readonly int DefaultMaxConnectNumber = 100;

        /// <summary>
        /// Default SendBufferSize
        /// </summary>
        public const int DefaultSendBufferSize = 1024 * 4;

        /// <summary>
        /// Default RecvBufferSize
        /// </summary>
        public const int DefaultRecvBufferSize = 1024 * 4;

        /// <summary>
        /// Default KeepAlivetime
        /// </summary>
        public readonly int DefaultKeepAliveTime = 60 * 10; // sec

        /// <summary>
        /// Default KeepAliveInterval
        /// </summary>
        public readonly int DefaultKeepAliveInterval = 60; // sec

        /// <summary>
        /// Default Per SendingQueue Size
        /// </summary>
        public readonly int DefaultSendingQueueSize = 5;

        /// <summary>
        /// Default worker threadpool mincount
        /// </summary>
        public readonly int DefaultMinWorkThreadCount = Environment.ProcessorCount;

        /// <summary>
        /// Default worker threadpool maxcount
        /// </summary>
        public readonly int DefaultMaxWorkThreadCount = 2000;

        /// <summary>
        /// Default IO threadpool mincount
        /// </summary>
        public readonly int DefaultMinIOThreadCount = Environment.ProcessorCount;

        /// <summary>
        /// Default IO threadpool maxcount
        /// </summary>
        public readonly int DefaultMaxIOThreadCount = 1000;

        /// <summary>
        /// Socket Option (Linger) flag
        /// </summary>
        public readonly bool DefaultSocketLingerFlag = false;

        /// <summary>
        /// Socket Option (Linger = true) DelayTime
        /// </summary>
        public readonly int DefaultSocketLingerDelayTime = 10;

        /// <summary>
        /// encoding
        /// </summary>
        public readonly string DefaultEncoding = Encoding.Default.ToString();

        //-------------------------------------------------------------------------

        // 1. Accpet Socket Recv/Send SocketAsyncEventArgs Pooling 갯수
        public int maxConnectNumber { get; set; }

        // 2. IP
        public string ip { get; set; }

        // 3. Port
        public ushort port { get; set; }

        // 4. KeepAliveTime 
        public int keepAliveTime { get; set; }

        // 5. KeepAliveInterval
        public int keepAliveInterval { get; set; }

        // 6. Naggle 알고리즘 사용 유뮤 (true:미사용, false:사용)
        public bool nodelay { get; set; }

        // 7. 소켓 타입 (tcp - udp)
        public eProtocolType protocolType { get; set; }

        // 8. Send용 버퍼사이즈
        public int sendBufferSize { get; set; }

        // 9. Recv용 버퍼사이즈
        public int recvBufferSize { get; set; }

        // 10. Per SendingQueue 사이즈
        public int sendingQueueSize { get; set; }

        // 12. Min Thread Count 
        public int minWorkThreadCount { get; set; }

        // 13. Max Thread Count
        public int maxWorkThreadCount { get; set; }

        // 14. Max Thread Count
        public int minIOThreadCount { get; set; }

        // 15. Max Thread Count
        public int maxIOThreadCount { get; set; }

        // 16. Socket Option (Linger) flag
        public bool socketLingerFlag { get; set; }

        // 17. Socket Option (Linger = true) DelayTime
        public int socketLingerDelayTime { get; set; }

        // 18. Encoding
        public string encoding { get; set; }

        public ServerConfig()
        {
            maxConnectNumber = DefaultMaxConnectNumber;
            keepAliveTime = DefaultKeepAliveTime;
            keepAliveInterval = DefaultKeepAliveInterval;
            sendBufferSize = DefaultSendBufferSize;
            recvBufferSize = DefaultRecvBufferSize;
            sendingQueueSize = DefaultSendingQueueSize;
            minWorkThreadCount = DefaultMinWorkThreadCount;
            maxWorkThreadCount = DefaultMaxWorkThreadCount;
            minIOThreadCount = DefaultMinIOThreadCount;
            maxIOThreadCount = DefaultMaxIOThreadCount;
            socketLingerFlag = DefaultSocketLingerFlag;
            socketLingerDelayTime = DefaultSocketLingerDelayTime;
            encoding = DefaultEncoding;         
        }
    }
    */
    #endregion
}
