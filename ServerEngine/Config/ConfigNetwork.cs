using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Config
{
    #region Config - Data
    public interface IConfigListen
    {
        public string address { get; }
        public ushort port { get; }
        public byte backlog { get; }
    }

    public class DefaultConfigListen : IConfigListen
    {
        public string address { get; } = string.Empty;
        public ushort port { get; }
        public byte backlog { get; }

        // 사용자정의 config 옵션멤버 추가

        public DefaultConfigListen(string address, ushort port, byte backlog)
        {
            this.address = address;
            this.port = port;
            this.backlog = backlog;
        }
    }

    public interface IConfigSocket
    {
        public int recv_buff_size { get; }
        public int send_buff_size { get; }
        public int recv_timeout { get; }
        public int send_timeout { get; }    
        public int send_queue_size { get; }
        public ushort heartbeat_start_time { get; }
        public ushort heartbeat_check_time { get; }
        public ushort linger_time { get; }
        public byte heartbeat_count { get; }
        public byte no_delay { get; }
    }

    public class DefaultConfigSocket : IConfigSocket
    {
        public int recv_buff_size { get; }
        public int send_buff_size { get; }
        public int recv_timeout { get; }
        public int send_timeout { get; }
        public int send_queue_size { get; }
        public ushort heartbeat_start_time { get; }
        public ushort heartbeat_check_time { get; }
        public ushort linger_time { get; }              // 0: false, 1 over : time / true
        public byte heartbeat_count { get; }
        public byte no_delay { get; }                   // 0: false, 1: true

        public DefaultConfigSocket(int recv_buff_size, int send_buff_size, int recv_timeout, int send_timeout,
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

    public interface IConfigSession
    {
        public long max_session_count { get; }
    }

    public class DefaultConfigSession : IConfigSession
    {
        public long max_session_count { get; }

        // 사용자정의 config 옵션멤버 추가

        public DefaultConfigSession(long max_session_count)
        {
            this.max_session_count = max_session_count;
        }
    }
    #endregion

    #region ConfigNetwork
    public interface IConfigNetwork
    {
        public string name { get; }
        public IConfigListen config_listen { get; }
        public IConfigSocket config_socket { get; }
        public IConfigSession config_session { get; }
    }

    public class DefaultConfigNetwork : IConfigNetwork
    {
        public string name { get; }
        public IConfigListen config_listen { get; }
        public IConfigSocket config_socket { get; }
        public IConfigSession config_session { get; }

        // 사용자정의 config 옵션멤버 추가

        public DefaultConfigNetwork(string name, IConfigListen config_listen, IConfigSocket config_socket, 
                                    IConfigSession config_session)
        {
            this.name = name;
            this.config_listen = config_listen;
            this.config_socket = config_socket;
            this.config_session = config_session;
        }
    }

    #endregion
}
