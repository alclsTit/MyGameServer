using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.SystemLib
{
    public interface IServerInfo
    {
        // 1. Accpet,Connect Socket Recv/Send SocketAsyncEventArgs Pooling 갯수
        int maxConnectNumber { get; }

        int keepAliveTime { get; }

        int keepAliveInterval { get; }

        // Naggle 알고리즘 사용 유뮤 (true:미사용, false:사용)
        bool nodelay { get; }

        // 통신 프로토콜 타입 (TCP / UDP / HTTP ...)
        eProtocolType protocolType { get; }

        // SEND 버퍼 사이즈
        int sendBufferSize { get; }

        // RECV 버퍼 사이즈 
        int recvBufferSize { get; }

        // Socket Option (Linger) flag
        bool socketLingerFlag { get; }

        // Socket Option (Linger = true) DelayTime
        int socketLingerDelayTime { get; }

        // Per SendingQueue 사이즈 
        int sendingQueueSize { get; }

        // ThreadPool count control flag
        bool controlThreadPoolCount { get; }

        // Min ThreadPool(work) Count 
        int minWorkThreadCount { get; }

        // Max ThreadPool(work) Count 
        int maxWorkThreadCount { get; }

        // Min ThreadPool(IO) Count 
        int minIOThreadCount { get; }

        // Max ThreadPool(IO) Count 
        int maxIOThreadCount { get; }

        // Encoding
        string encoding { get; }
    }


    #region "삭제예정 백업본"
    /// <summary>
    /// Personal Server specific feature
    /// </summary>
    /// Todo: ini 설정파일로 서버 옵션 읽어들여서 서버별 세팅 및 사용예정 
    /*public interface IServerInfo
    {
        // 1. Accpet Socket Recv/Send SocketAsyncEventArgs Pooling 갯수
        int maxConnectNumber { get; }

        // 2. IP
        string ip { get; }

        // 3. Port
        ushort port { get; }

        // 4. KeepAliveTime 
        int keepAliveTime { get; }

        // 5. KeepAliveInterval
        int keepAliveInterval { get; }

        // 6. Naggle 알고리즘 사용 유뮤 (true:미사용, false:사용)
        bool nodelay { get; }

        // 7. 소켓 타입 (tcp - udp)
        eProtocolType protocolType { get; }

        // 8. Send용 버퍼사이즈
        int sendBufferSize { get; }

        // 9. Recv용 버퍼사이즈
        int recvBufferSize { get; }

        // 10. Per SendingQueue 사이즈 
        int sendingQueueSize { get; }

        // 12. Min ThreadPool(work) Count 
        int minWorkThreadCount { get; }

        // 13. Max ThreadPool(work) Count 
        int maxWorkThreadCount { get; }

        // 14. Min ThreadPool(IO) Count 
        int minIOThreadCount { get; }

        // 15. Max ThreadPool(IO) Count 
        int maxIOThreadCount { get; }

        // 16. Socket Option (Linger) flag
        bool socketLingerFlag { get; }

        // 17. Socket Option (Linger = true) DelayTime
        int socketLingerDelayTime { get; }

        // 18. Encoding
        string encoding { get; }
    }
    */
    #endregion
}
