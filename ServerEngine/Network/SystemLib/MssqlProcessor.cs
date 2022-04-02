using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace ServerEngine.Network.SystemLib
{
    public static class MssqlProcessor
    {
        private static SqlConnection Connect(eMSSQL_DBTYPE dbName)
        {
            if (MssqlConfigManager.IsExist(dbName))
            {
                var connection = new SqlConnection(MssqlConfigManager.msDBConfigMap[dbName].GetConnectionString());
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                return connection;
            }

            return null;
        }

        private static SqlConnection ConnectAsync(eMSSQL_DBTYPE dbName)
        {
            if (MssqlConfigManager.IsExist(dbName))
            {
                var connection = new SqlConnection(MssqlConfigManager.msDBConfigMap[dbName].GetConnectionString());
                if (connection.State != ConnectionState.Open)
                    connection.OpenAsync();

                return connection;
            }

            return null;
        }
    }
}
