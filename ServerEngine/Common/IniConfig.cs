using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

/// <summary>
/// GameLib는 시스템 전반에서 공용으로 사용되는 라이브러리 
/// </summary>
namespace ServerEngine.Common
{
    public static class IniConfig
    {
        private const int MAXLEN = 255;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long GetPrivateProfileString(string section, string key, string def_value, StringBuilder retval, int size, string filePath);

        private static readonly object mLockObj = new object();

        /// <summary>
        /// Write xxx.ini file (non-threadsafe)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="value"></param>
        /// <param name="filepath"></param>
        public static void IniFileWrite(string section, string key, string value, string filepath)
        {
            WritePrivateProfileString(section, key, value, filepath);
        }

        /// <summary>
        /// Read xxx.ini file (non-threadsafe)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string IniFileRead(string section, string key, string value, string filepath)
        {
            var readInfo = new StringBuilder(MAXLEN);

            GetPrivateProfileString(section, key, value, readInfo, MAXLEN, filepath);
            return readInfo.ToString().Trim();
        }

        /// <summary>
        /// Write xxx.ini file (threadsafe)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="value"></param>
        /// <param name="filepath"></param>
        public static void IniFileWriteThreadSafe(string section, string key, string value, string filepath)
        {
            lock(mLockObj)
            {
                WritePrivateProfileString(section, key, value, filepath);
            }
        }

        /// <summary>
        /// Read xxx.ini file (threadsafe)
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="filepath"></param>
        /// <returns></returns>
        public static string IniFileReadThreadSafe(string section, string key, string value, string filepath)
        {
            var readInfo = new StringBuilder(MAXLEN);

            lock(mLockObj)
            {
                GetPrivateProfileString(section, key, value, readInfo, MAXLEN, filepath);
            }

            return readInfo.ToString().Trim();
        }

    }
}
