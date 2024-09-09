using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ServerEngine.Network.SystemLib;
using ServerEngine.Common;

namespace ServerEngine.Config
{
    // 24.09.09 삭제 예정
    /*
    /// <summary>
    /// Config 관련 로드작업에 사용되는 추상클래스
    /// </summary>
    /// <typeparam name="Class"></typeparam>
    /// <typeparam name="ConfigInfo"></typeparam>
    public abstract class ConfigLoaderBase<Class, ConfigInfo> : IConfigLoader<ConfigInfo>
        where Class : ConfigLoaderBase<Class, ConfigInfo> 
        where ConfigInfo : ServerConfig, new()
    {
        /// <summary>
        /// 해당 ConfigLoader를 상속한 클래스들 또한 전역으로 한번만 생성되는 것을 요구하기 때문에 추상클래스에서 싱글톤으로 선언 (게으른 생성)
        /// </summary>
        private static readonly Lazy<ConfigLoaderBase<Class, ConfigInfo>> msInstance = new Lazy<ConfigLoaderBase<Class, ConfigInfo>>(() => Activator.CreateInstance(typeof(Class), true) as Class);
        public static ConfigLoaderBase<Class, ConfigInfo> Instance => msInstance.Value;

        /// <summary>
        /// Config 파일이 저장된 경로
        /// </summary>
        public string mFilePath { get; private set; } = "";

        /// <summary>
        /// 싱글톤 객체이므로 파생클래스에서 생성자의 접근제한자는 반드시 private이여야 한다
        /// </summary>
        protected ConfigLoaderBase() { }

        /// <summary>
        /// 1. 파일 이름 및 경로 세팅, 파일이 존재하는지 확인 및 config 세팅
        /// 2. Config 폴더 생성위치는 MyGameServer 프로젝트 하위
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileExtension">Read할 파일 확장자. default는 ini 파일</param>
        public void Initialize(string fileName, eFileExtension fileExtension = eFileExtension.INI)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            string lFileExtension = "";
            switch (fileExtension)
            {
                case eFileExtension.INI:
                    lFileExtension = "ini";
                    break;
                case eFileExtension.TXT:
                    lFileExtension = "txt";
                    break;
                default:
                    lFileExtension = "ini";
                    break;
            }

            var folderPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\..\Config"));
            var filePath = folderPath + $"\\{fileName}.{lFileExtension}";

            if (!File.Exists(filePath))
            {
                throw new DirectoryNotFoundException($"Can't to load file [PATH: {filePath}]");
            }

            mFilePath = filePath;
        }

        /// <summary>
        /// Config 파일에서 읽은 옵션 세팅 (서버 옵션 공통사항 로드 및 세팅)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        public virtual bool LoadConfig(out ConfigInfo item)
        {
            var key = "ServerInfo";
            var config = new ConfigInfo();

            // 서버별 Connect 가능한 클라이언트 수 
            config.maxConnectNumber = Convert.ToInt32(IniConfig.IniFileRead(key, "Max_Connect_Number", $"{config.maxConnectNumber}", mFilePath));

            // 하트비트 시작 기준 시간(초)
            config.keepAliveTime = Convert.ToInt32(IniConfig.IniFileRead(key, "Keep_Alive_Time", $"{config.keepAliveTime}", mFilePath));

            // 하트비트 보내는 횟수
            config.keepAliveInterval = Convert.ToInt32(IniConfig.IniFileRead(key, "Keep_Alive_Interval", $"{config.keepAliveInterval}", mFilePath));

            // nagle 알고리즘 사용유무
            config.nodelay = Convert.ToBoolean(IniConfig.IniFileRead(key, "Nodelay", $"{config.nodelay}", mFilePath));

            // 프로토콜 타입
            var protocolTypeString = IniConfig.IniFileRead(key, "ProtocolType", $"{config.protocolType}", mFilePath);
            config.protocolType = GlobalFunction.GetProtocolType(protocolTypeString);

            // Send 전용 버퍼사이즈 
            config.sendBufferSize = Convert.ToInt32(IniConfig.IniFileRead(key, "Send_Buffer_Size", $"{config.sendBufferSize}", mFilePath));

            // Recv 전용 버퍼사이즈 
            config.recvBufferSize = Convert.ToInt32(IniConfig.IniFileRead(key, "Recv_Buffer_Size", $"{config.recvBufferSize}", mFilePath));

            // Send Queue 사이즈 
            config.sendingQueueSize = Convert.ToInt32(IniConfig.IniFileRead(key, "Send_Queue_Size", $"{config.sendingQueueSize}", mFilePath));

            // 최소 스레드풀 워커 스레드 갯수 
            config.minWorkThreadCount = Convert.ToInt32(IniConfig.IniFileRead(key, "Min_WorkThread_Count", $"{config.DefaultMinWorkThreadCount}", mFilePath));

            // 최대 스레드풀 워커 스레드 갯수 
            config.maxWorkThreadCount = Convert.ToInt32(IniConfig.IniFileRead(key, "Max_WorkThread_Count", $"{config.DefaultMaxWorkThreadCount}", mFilePath));

            // 최소 스레드풀 IO 스레드 갯수
            config.minIOThreadCount = Convert.ToInt32(IniConfig.IniFileRead(key, "Min_IOThread_Count", $"{config.DefaultMinIOThreadCount}", mFilePath));

            // 최대 스레드풀 IO 스레드 갯수
            config.maxIOThreadCount = Convert.ToInt32(IniConfig.IniFileRead(key, "Max_IOThread_Count", $"{config.DefaultMaxIOThreadCount}", mFilePath));

            // Session Socket Linger Option (false = 소켓 Close 요청 후 대기하지않고 Close, true = 소켓 Close 요청 후 일정시간 대기 후 Close)
            config.socketLingerFlag = Convert.ToBoolean(IniConfig.IniFileRead(key, "Socket_Close_Delay", $"{config.socketLingerFlag}", mFilePath));

            // Session Socket Linger Option (True) 일 때, delay 시간
            config.socketLingerDelayTime = Convert.ToInt32(IniConfig.IniFileRead(key, "Socket_Close_DelayTime", $"{config.socketLingerDelayTime}", mFilePath));

            // Encoding 방식
            config.encoding = IniConfig.IniFileRead(key, "Encoding", $"{config.encoding}", mFilePath);

            // ThreadPool Control flag
            config.controlThreadPoolCount = Convert.ToBoolean(IniConfig.IniFileRead(key, "Control_ThreadPool", $"{config.controlThreadPoolCount}", mFilePath));

            // Send Timeout 세팅
            config.sendTimeout = Convert.ToInt32(IniConfig.IniFileRead(key, "Send_Timeout", $"{config.sendTimeout}", mFilePath));

            // Recv Timeout 세팅
            config.recvTimeout = Convert.ToInt32(IniConfig.IniFileRead(key, "Recv_Timeout", $"{config.recvTimeout}", mFilePath));

            item = config;
            return true;
        }

        /// <summary>
        /// [주의] 해당 메서드 구현은 RelayServer에서만 진행. 다른 파생클래스에서는 NotSupportedException 익셉션을 던진다
        /// Config 파일을 파생클래스에서 구현하기 때문에 해당 부분도 파생클래스에서 구현해줘야한다
        /// </summary>
        /// <param name="listeners"></param>
        /// <returns></returns>
        public virtual bool LoadListeners(List<IListenInfo> listeners)
        {
            var key = "ConnectInfo";
            var numOfConnectedServer = Convert.ToInt32(IniConfig.IniFileRead(key, "Connect_Server", "0", mFilePath));

            if (numOfConnectedServer >= 1)
            {
                if (listeners.Count > 0)
                    listeners.Clear();

                for (var idx = 1; idx <= numOfConnectedServer; ++idx)
                {
                    var ip          = IniConfig.IniFileRead(key, $"Server_IP_{idx}", "127.0.0.1", mFilePath);
                    var port        = Convert.ToUInt16(IniConfig.IniFileRead(key, $"Server_Port_{idx}", "8800", mFilePath));
                    // 메인 서버에 연결된 다른 서버들의 이름을 지정
                    var name        = IniConfig.IniFileRead(key, $"Server_Name_{idx}", "Default", mFilePath);
                    var backlog     = Convert.ToInt32(IniConfig.IniFileRead(key, $"Server_Backlog_{idx}", $"{ServerDefaultOption.backlog}", mFilePath));

                    // 기본적으로 nagle 알고리즘을 사용하지 않는다. 즉 패킷을 모아서 보내지 않는다
                    IListenInfo listenInfo = new ListenConfig(ip, port, name, backlog);
                    listeners.Add(listenInfo);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual bool LoadMssqlConfig(string dbHostName, ushort port = 1433, bool ipv4 = true)
        {
            return true;
        }

        public virtual bool LoadMysqlConfig(string dbHostName, ushort port = 3306, bool ipv4 = true)
        {
            return true;
        }
    }
    */
}
