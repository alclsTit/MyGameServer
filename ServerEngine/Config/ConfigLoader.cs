using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using ServerEngine.Common;
using ServerEngine.Network.SystemLib;

namespace ServerEngine.Config
{
    /// <summary>
    /// 프로세스당 config 옵션파일 로더는 한개뿐이므로 싱클턴 패턴 적용
    /// </summary>
    /*public class ConfigLoader
    {
        public string mFilePath { get; private set; } = "";
        
        public static readonly ConfigLoader Instance = new ConfigLoader();

        /// <summary>
        /// 외부에서 인스턴스를 생성하지 못하도록 객체의 생성자 숨김
        /// </summary>
        private ConfigLoader() {}

        /// <summary>
        /// 파일 이름 및 경로 세팅, 파일이 존재하는지 확인 및 config 세팅
        /// </summary>
        /// <param name="configFileName"></param>
        /// <param name="fileExtension"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public void Initialize(string name, eFileExtension fileExtension = eFileExtension.INI)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

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

            var folderPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Config"));
            var filePath = folderPath + $"\\{name}.{lFileExtension}";
            
            if (!File.Exists(filePath))
            {
                throw new DirectoryNotFoundException($"Can't to load file [PATH: {filePath}]");
            }

            mFilePath = filePath;
        }

        /// <summary>
        /// Config 옵션파일을 실질적으로 세팅하는 부분
        /// </summary>
        /// <param name="listeners"></param>
        /// <param name="serverConfig"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public bool LoadConfig(List<IListenInfo> listeners, out IServerInfo serverConfig)
        {
            if (listeners == null) 
                throw new ArgumentNullException(nameof(listeners));

            if (string.IsNullOrEmpty(mFilePath))
                throw new ArgumentNullException(nameof(mFilePath));

            // [ConnectInfo] ini Section
            var lConnectInfoSection = "ConnectInfo";
            var lCountOfListener = Convert.ToInt32(IniConfig.IniFileRead(lConnectInfoSection, "Connect_Server", "0", mFilePath));

            if (lCountOfListener >= 1)
            {
                if (listeners.Count > 0)
                    listeners.Clear();
            }
            else
            {
                throw new Exception($"Count of ConnectServer is abnormal - {lCountOfListener}");
            }

            // [ServerInfo] ini Section
            var lServerInfoSection = "ServerInfo";
            var config = new ServerConfig();

            // BackLog
            config.listenBacklog = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Listen_Backlog", $"{config.listenBacklog}", mFilePath));

            for (var idx = 1; idx <= lCountOfListener; ++idx)
            {
                var ip = IniConfig.IniFileRead(lConnectInfoSection, $"Server_IP_{idx}", "127.0.0.1", mFilePath);
                var port = Convert.ToUInt16(IniConfig.IniFileRead(lConnectInfoSection, $"Server_Port_{idx}", "8800", mFilePath));
                var name = IniConfig.IniFileRead(lConnectInfoSection, $"Server_Name_{idx}", "Default", mFilePath);

                // 기본적으로 nagle 알고리즘을 사용하지 않는다. 즉 패킷을 모아서 보내지 않는다
                IListenInfo lListenConfig = new ListenConfig(ip, port, config.listenBacklog, name);
                listeners.Add(lListenConfig);
            }

            // 서버별 Connect 가능한 클라이언트 수 
            config.maxConnectNumber = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Max_Connect_Number", $"{config.maxConnectNumber}", mFilePath));

            // 하트비트 시작 기준 시간(초)
            config.keepAliveTime = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Keep_Alive_Time", $"{config.keepAliveTime}", mFilePath));

            // 하트비트 보내는 횟수
            config.keepAliveInterval = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Keep_Alive_Interval", $"{config.keepAliveInterval}", mFilePath));

            // Send 전용 버퍼사이즈 
            config.sendBufferSize = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Send_Buffer_Size", $"{config.sendBufferSize}", mFilePath));

            // Recv 전용 버퍼사이즈 
            config.recvBufferSize = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Recv_Buffer_Size", $"{config.recvBufferSize}", mFilePath));

            // Send Queue 사이즈 
            config.sendingQueueSize = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Send_Queue_Size", $"{config.sendingQueueSize}", mFilePath));

            // 최소 스레드풀 워커 스레드 갯수 
            config.minWorkThreadCount = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Min_WorkThread_Count", $"{config.DefaultMinWorkThreadCount}", mFilePath));

            // 최대 스레드풀 워커 스레드 갯수 
            config.maxWorkThreadCount = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Max_WorkThread_Count", $"{config.DefaultMaxWorkThreadCount}", mFilePath));

            // 최소 스레드풀 IO 스레드 갯수
            config.minIOThreadCount = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Min_IOThread_Count", $"{config.DefaultMinIOThreadCount}", mFilePath));

            // 최대 스레드풀 IO 스레드 갯수
            config.maxIOThreadCount = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Max_IOThread_Count", $"{config.DefaultMaxIOThreadCount}", mFilePath));

            // Session Socket Linger Option (false = 소켓 Close 요청 후 대기하지않고 Close, true = 소켓 Close 요청 후 일정시간 대기 후 Close)
            config.socketLingerFlag = Convert.ToBoolean(IniConfig.IniFileRead(lServerInfoSection, "Socket_Close_Delay", $"{config.socketLingerFlag}", mFilePath));

            // Session Socket Linger Option (True) 일 때, delay 시간
            config.socketLingerDelayTime = Convert.ToInt32(IniConfig.IniFileRead(lServerInfoSection, "Socket_Close_DelayTime", $"{config.socketLingerDelayTime}", mFilePath));

            // 서버 이름
            config.serverName = IniConfig.IniFileRead(lServerInfoSection, "Server_Name", $"{config.serverName}", mFilePath);

            // Encoding 방식
            config.encoding = IniConfig.IniFileRead(lServerInfoSection, "Encoding", $"{config.encoding}", mFilePath);   

            serverConfig = config;

            return true;
        }

    }
    */

    public static class ConfigLoader
    {
        public enum eFileExtensionType
        {
            none = 0,
            json = 1,
            Max = 2
        }

        private static string? Find(in string file_name, in eFileExtensionType type)
        {
            try
            {
                // 현재 실행중인 프로세스의 실행파일이 있는 위치 로드
                var root_path = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName);
                if (string.IsNullOrEmpty(root_path))
                    return null;

                string filename_extension;
                switch (type)
                {
                    case eFileExtensionType.json:
                        filename_extension = $"{file_name}.json";
                        break;
                    default:
                        return null;
                }

                // 폴더 존재유무 체크 및 없다면 생성
                string folder_path = Path.Join(root_path, "data");
                if (!Directory.Exists(folder_path))
                    Directory.CreateDirectory(folder_path);
          
                // 파일 존재유무 체크 및 없다면 생성
                string file_path = Path.Join(folder_path, filename_extension);
                if (!File.Exists(file_path))
                    File.Create(file_path);

                return file_path;
            }
            catch (Exception)
            {
                throw;
            }
        }

        #region Json
        private static T? ConvertJsonToObject<T>(in string file_path) where T : class
        {
            try
            {
                if (string.IsNullOrEmpty(file_path))
                    return default(T);

                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(File.ReadAllText(file_path));
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static T? LoadJson<T>(in string file_name, in eFileExtensionType type) where T : class
        {
            try
            {
                var filepath = Find(file_name, type);
                if (null == filepath)
                    return default;

                switch (type)
                {
                    case eFileExtensionType.json:
                        return ConvertJsonToObject<T>(filepath);
                    default:
                        return default;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion
    }
}
