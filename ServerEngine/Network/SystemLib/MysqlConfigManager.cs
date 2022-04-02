using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// MYSQL Server 관련 Connection에 사용되는 정보관리 클래스
    /// 초기에 한번 생성하고 프로그램 생명 주기동안 삭제할 일이 없어 non thread-safe하게 구현 
    /// MssqlProcessor에서 접근하여 사용
    /// </summary>
    public static class MysqlConfigManager
    {
        public static Dictionary<eMYSQL_DBTYPE, MysqlConfig> msDBConfigMap { get; private set; } = new Dictionary<eMYSQL_DBTYPE, MysqlConfig>();

        public static bool IsExist(eMYSQL_DBTYPE dbName) => msDBConfigMap.ContainsKey(dbName);

        public static int Count => msDBConfigMap.Count;

        public static bool TryAdd(eMYSQL_DBTYPE dbName, MysqlConfig config)
        {
            return msDBConfigMap.TryAdd(dbName, config);
        }

        // 사용 안하지만, 혹시 몰라서 남겨둠
        private static bool TryRemove(eMYSQL_DBTYPE dbName)
        {
            if (!IsExist(dbName))
                return false;

            msDBConfigMap.Remove(dbName);

            return true;
        }

        public static string GetConnectionString(eMYSQL_DBTYPE dbName)
        {
            if (dbName <= eMYSQL_DBTYPE._MIN_NO_TYPE || dbName >= eMYSQL_DBTYPE._MAX_NO_TYPE)
                return default(string);

            if (msDBConfigMap.TryGetValue(dbName, out var config))
                return config.GetConnectionString();
            else
                return default(string);
        }

        public static IEnumerable<MysqlConfig> GetEnumerator()
        {
            using (var enumerator = msDBConfigMap.Values.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    yield return enumerator.Current;
                }
            }
        }
    }
}
