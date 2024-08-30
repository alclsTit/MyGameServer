using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Config
{
    /*#region ConfigEtc - Log
    public class ConfigLog
    {
        public readonly string name;
        public readonly int file_minlevel;
        public readonly int console_minlevel;
        public readonly int rolling_interval;

        public ConfigLog(string name, int file_minlevel, int console_minlevel, int rolling_interval)
        {
            this.name = name.ToLower().Trim();
            this.file_minlevel = file_minlevel;
            this.console_minlevel = console_minlevel;
            this.rolling_interval = rolling_interval;
        }
    }

    public class Logger
    {
        public readonly List<ConfigLog> list = new List<ConfigLog>();
    }
    #endregion

    #region ConfigEtc - Pool
    public class ConfigPool
    {
        public readonly string name = string.Empty;
        public readonly long defalut_size;
        public readonly long create_size;

        public ConfigPool(string name, long defalut_size, long create_size)
        {
            this.name = name.ToLower().Trim();
            this.defalut_size = defalut_size;
            this.create_size = create_size;
        }
    }

    public class Pool
    {
        public readonly Dictionary<string, ConfigPool> pools = new Dictionary<string, ConfigPool>();
    }

    #endregion

    public class ConfigEtc
    {
        public readonly string name = string.Empty;
        public readonly Logger logger = new Logger();
    }

    #region ConfigNetwork - Socket
    public class ConfigSocket
    {
        public readonly int recv_buff_size;
        public readonly int send_buff_size;
        public readonly int recv_timeout;
        public readonly int send_timeout;
        public readonly int send_queue_size;
        public readonly ushort heartbeat_start_time;
        public readonly ushort heartbeat_check_time;
        public readonly ushort linger_time;             // 0: false, 1 over : time / true
        public readonly byte heartbeat_count;
        public readonly byte no_delay;                  // 0: false, 1: true
    
        public ConfigSocket(int recv_buff_size, int send_buff_size, int recv_timeout, int send_timeout,
                            int send_queue_size, ushort heartbeat_start_time, ushort heartbeat_check_time,
                            ushort linger_time, byte heartbeat_count, byte no_delay)
        {
            this.recv_buff_size = recv_buff_size;
            this.send_buff_size = send_buff_size;
            this.recv_timeout = recv_timeout;
            this.send_timeout = send_timeout;
            this.send_queue_size = send_queue_size;
            this.heartbeat_start_time = heartbeat_start_time;
            this.heartbeat_check_time = heartbeat_check_time;
            this.linger_time = linger_time;
            this.heartbeat_count = heartbeat_count;
            this.no_delay = no_delay;
        }
    }
    #endregion

    #region ConfigNetwork - Session
    public class ConfigSession
    {
        public readonly long max_session_count;
        public ConfigSession(long max_session_count)
        {
            this.max_session_count = max_session_count;
        }
    }
    #endregion

    #region Config - Network
    public class ConfigNetwork
    {
        public readonly ConfigListen config_listen;
        public readonly ConfigSocket config_socket;
        public readonly ConfigSession config_session;

        public readonly string name;

        public ConfigNetwork(string name, in ConfigListen config_listen, in ConfigSocket config_socket, in ConfigSession config_session)
        {
            this.name = name;

            this.config_listen = config_listen;
            this.config_socket = config_socket;
            this.config_session = config_session;
        }
    }
    #endregion
    */

    #region ConfigLoader Function
    //class ConfigCommon
    //{
    //    public bool LoadConfig<TConfig>(string file_name, ConfigLoader.eFileExtensionType type, [NotNullWhen(true)] out TConfig? config) 
    //        where TConfig : class, new()
    //    {
    //        if (ConfigLoader.eFileExtensionType.none <= type || ConfigLoader.eFileExtensionType.Max >= type)
    //            throw new ArgumentOutOfRangeException(nameof(type));
    //
    //        config = ConfigLoader.LoadJson<TConfig>(file_name, type);
    //        return null != config ? true : false;
    //    }
    //}

    /// <summary>
    /// json 형태의 config 파일을 모두 로드해오는 default 옵션
    /// </summary>
    public class DefaultConfigLoader
    {

        public void LoadConfigAll()
        {

        }
    }

    public interface IConfigCommon
    {
        public IConfigNetwork config_network { get; }
        public IConfigEtc config_etc { get; }
    }

    public class ConfigCommon : IConfigCommon
    {
        public IConfigNetwork config_network { get; }
        public IConfigEtc config_etc { get; }

        public ConfigCommon(IConfigNetwork config_network, IConfigEtc config_etc)
        {
            this.config_network = config_network;
            this.config_etc = config_etc;
        }
    }

    #endregion
}

