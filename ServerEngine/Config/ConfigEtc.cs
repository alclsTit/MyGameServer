using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Config
{
    #region Config - Data
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
        public List<IConfigPool> list { get; }
    }
    public class DefaultPool : IPool
    {
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
