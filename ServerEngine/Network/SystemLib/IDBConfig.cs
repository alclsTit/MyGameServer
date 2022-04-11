using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.SystemLib
{
    public struct sDBResultModify
    {
        public bool success { get; private set; } = false;
        public int affectedRows { get; private set; } = 0;
        public int output { get; private set; } = 0;

        public sDBResultModify(bool flag = false, int affected = 0, int output = 0)
        {
            success = flag;
            affectedRows = affected;
            this.output = output;
        }
    }

    public struct sDBResultSelect 
    {
        public bool success { get; private set; } = false;
        public int affectedRows { get; private set; } = 0;
        public System.Data.DataTable selectTable { get; private set; } = null;
        public int output { get; private set; } = 0;

        public sDBResultSelect(bool flag = false, int affected = 0, System.Data.DataTable table = null, int output = 0)
        {
            success = flag;
            affectedRows = affected;
            selectTable = table;
            this.output = output;
        }
    }    

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
