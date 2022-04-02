using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.SystemLib
{
    public enum eMSSQL_DBTYPE
    {
        _MIN_NO_TYPE = 0,
        LAB_GAME01 = 1,
        LAB_GAME02 = 2,
        _MAX_NO_TYPE = 3
    }

    public enum eMYSQL_DBTYPE
    {
        _MIN_NO_TYPE = 0,
        _MAX_NO_TYPE = 1
    }

    public interface IDBConfig
    {
        public string ip { get; }

        public string port { get; }

        public string loginID { get; }

        public string loginPW { get; }

    }
}
