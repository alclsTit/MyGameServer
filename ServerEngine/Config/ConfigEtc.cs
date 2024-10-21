using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Config
{
    #region Config - Data
    public interface IRedisAddress
    {
        public string ip { get; }
        public int port { get; }
        public string url { get; }
        public int db_index { get; }
    }

    public interface IConfigRedis
    {
        public string name { get; }
        public IRedisAddress address { get; }
        public string password { get; }
        public bool ssl { get; }
        public int keep_alive_sec { get; }
        public int connect_retry { get; }
        public int reconnect_timeout_sec { get; }
        public int sync_timeout_sec { get; }
    }

    public interface IConfigMultiRedis
    {
        public string name { get; }
        public IList<IRedisAddress> list_address { get; }
        public string password { get; }
        public bool ssl { get; }
        public int keep_alive_sec { get; }
        public int connect_retry { get; }
        public int reconnect_timeout_sec { get; }
        public int sync_timeout_sec { get; }
    }

    public class DefaultConfigRedis : IConfigRedis
    {
        public string name { get; }
        public IRedisAddress address { get; }
        public string password { get; }
        public bool ssl { get; }
        public int keep_alive_sec { get;}
        public int connect_retry { get;}  
        public int reconnect_timeout_sec { get;}    
        public int sync_timeout_sec { get;}
        public DefaultConfigRedis(string name, IRedisAddress address, string password, 
                                  bool ssl, int keep_alive_sec, int connect_retry, 
                                  int reconnect_timeout_sec, int sync_timeout_sec)
        {
            this.name = name;
            this.address = address;
            this.password = password;
            this.ssl = ssl;
            this.keep_alive_sec = keep_alive_sec;
            this.connect_retry = connect_retry;
            this.reconnect_timeout_sec = reconnect_timeout_sec;
            this.sync_timeout_sec = sync_timeout_sec;
        }
    }

    public class DefaultConfigMultiRedis : IConfigMultiRedis
    {
        public string name { get; }
        public IList<IRedisAddress> list_address { get; }
        public string password { get; }
        public bool ssl { get; }
        public int keep_alive_sec { get; }
        public int connect_retry { get; }
        public int reconnect_timeout_sec { get; }
        public int sync_timeout_sec { get; }
        public DefaultConfigMultiRedis(string name, IList<IRedisAddress> list_address, string password,
                                       bool ssl, int keep_alive_sec, int connect_retry,
                                       int reconnect_timeout_sec, int sync_timeout_sec)
        {
            this.name = name;
            this.list_address = list_address;
            this.password = password;
            this.ssl = ssl;
            this.keep_alive_sec = keep_alive_sec;
            this.connect_retry = connect_retry;
            this.reconnect_timeout_sec = reconnect_timeout_sec;
            this.sync_timeout_sec = sync_timeout_sec;
        }
    }

    public interface IConfigLog
    {
        public string name { get; }
        public int file_minlevel { get; }
        public int console_minlevel { get; }
        public int rolling_interval { get; }
    }
    public class DefaultConfigLog : IConfigLog
    {
        public string name { get; }
        public int file_minlevel { get; }   
        public int console_minlevel { get; }
        public int rolling_interval { get; }    
        public DefaultConfigLog(string name, int file_minlevel, int console_minlevel, int rolling_interval)
        {
            this.name = name;
            this.file_minlevel = file_minlevel;
            this.console_minlevel = console_minlevel;
            this.rolling_interval = rolling_interval;
        }
    }
    public interface ILogger
    {
        public List<IConfigLog> list { get; } 
    }
    public class DefaultLogger : ILogger
    {
        public List<IConfigLog> list { get; }
        public DefaultLogger(List<IConfigLog> list)
        {
            this.list = list;
        }
    }

    public interface IConfigPool
    {
        public string name { get; }
        public int default_size { get; }
        public int create_size { get; }
    }
    public class DefaultConfigPool : IConfigPool
    {
        public string name { get; }
        public int default_size { get; }   
        public int create_size { get; }
        public DefaultConfigPool(string name, int default_size, int create_size)
        {
            this.name = name;
            this.default_size = default_size;
            this.create_size = create_size;
        }
    }
    public interface IPool
    {
        public string name { get; }
        public List<IConfigPool> list { get; }
    }
    public class DefaultPool : IPool
    {
        public string name { get; }
        public List<IConfigPool> list { get; }
        public DefaultPool(List<IConfigPool> list)
        {
            this.list = list;
        }
    }
    #endregion

    #region ConfigEtc
    public interface IConfigEtc
    {
        public string name { get; }
        public IPool pools { get; }
        public ILogger logger { get; }
    }

    public class DefaultConfigEtc : IConfigEtc
    {
        public string name { get; }
        public IPool pools { get; }
        public ILogger logger { get; }

        public DefaultConfigEtc(string name, IPool pools, ILogger logger)
        {
            this.name = name;
            this.pools = pools;
            this.logger = logger;
        }
    }
    #endregion
}
