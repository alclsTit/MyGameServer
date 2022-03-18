using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 전역에서 사용되는 Constant 멤버들 정의
namespace ServerEngine
{
    /// <summary>
    /// Config 파일로드시 사용되는 파일 확장자 열거형
    /// </summary>
    public enum eFileExtension
    {
        INI = 0,
        TXT = 1
    }

    /// <summary>
    /// 소켓 통신 프로토콜 열거형
    /// </summary>
    public enum eProtocolType
    {
        UNKNOWN = 0,
        TCP = 1,
        UDP = 2,
        HTTP = 3
    }

    /// <summary>
    /// 세션 종료에 대한 원인 정의
    /// </summary>
    public enum eCloseReason
    {
        Unknown = 0,
        ServerShutdown = 1,
        ClientClose = 2,
        Timeout = 4,
        SocketError = 5,
        SeverException = 6
    }

    /// <summary>
    /// 소켓 상태 (값 하나에 데이터가 중복저장되어야 하므로 비트연산으로 처리)
    /// </summary>
    public static class SocketState
    {
        public const int NotInitialized = 0;

        public const int Sending = 4;

        public const int Receiving = 8;

        public const int Closing = 16;

        public const int Closed = 32;
    }

    /// <summary>
    /// 서버 상태 Constant 멤버 
    /// </summary>
    public static class ServerState
    {
        public const int NotInitialized = 0;

        public const int Initialized = 1;

        public const int SetupFinished = 2;

        public const int NotStarted = 3;

        public const int Running = 4;

        public const int Stopped = 5;
    }

    /// <summary>
    /// 시스템 관련(accept, connect) 상태 Constant 멤버
    /// </summary>
    public static class NetworkSystemState
    {
        public const int NotInitialized = 0;

        public const int Initialized = 1;

        public const int Running = 2;

        public const int Stopping = 3;

        public const int StopCompleted = 4;
    }

    /// <summary>
    /// 서버에서 사용하는 옵션 중 기본 값 설정
    /// </summary>
    public static class ServerDefaultOption
    {
        public const int backlog = 200;
    }

    /// <summary>
    /// 패킷 헤더 정보
    /// </summary>
    public static class PacketHeaderInfo
    {
        public const int MAX_PACKET_HEADER_SIZE = 2;
        public const int MAX_PACKET_HEADER_ID = 2;
        public const int MAX_PACKET_HEADER_TICKCOUNT = 8;
    }
}
