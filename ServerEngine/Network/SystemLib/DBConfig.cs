using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.SystemLib
{
    public abstract class DBConfig : IDBConfig
    {
        public string ip { get; private set; }

        public string port { get; private set; }

        public string loginID { get; private set; }

        public string loginPW { get; private set; }      

        public DBConfig(string ip, string port, string loginID, string loginPW)
        {
            this.ip = ip;
            this.port = port;
            this.loginID = loginID;
            this.loginPW = loginPW;
        }

        public abstract string GetConnectionString();

    }

    public sealed class MssqlConfig : DBConfig
    {
        private readonly string mConnectionString;

        public MssqlConfig(eMSSQL_DBTYPE dbType, string ip, string port, string loginID, string loginPW)
            : base(ip, port, loginID, loginPW) 
        {
            mConnectionString = $"Data Source={ip},{port};Initial Catalog={dbType};User ID={loginID};Password={loginPW}";
        }

        public MssqlConfig(eMSSQL_DBTYPE dbType, string ip, string loginPW) 
            : base(ip, "1433", "sa", loginPW) 
        {
            mConnectionString = $"Data Source={ip},1433;Initial Catalog={dbType};User ID=sa;Password={loginPW}";
        }

        public override string GetConnectionString()
        {
            return mConnectionString;
        }
    }

    public sealed class MysqlConfig : DBConfig
    {
        private readonly string mConnectionString;

        public MysqlConfig(eMYSQL_DBTYPE dbType, string ip, string port, string loginID, string loginPW)
            : base(ip, port, loginID, loginPW) 
        {
            mConnectionString = $"Data Source={ip},{port};Initial Catalog={dbType};User ID={loginID};Password={loginPW}";
        }

        public MysqlConfig(eMYSQL_DBTYPE dbType, string ip, string loginPW)
            : base(ip, "3306", "root", loginPW) 
        {
            mConnectionString = $"Data Source={ip},3306;Initial Catalog={dbType};User ID=root;Password={loginPW}";
        }

        public override string GetConnectionString()
        {
            return mConnectionString;
        }
    }
}
