using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerEngine.Config;

namespace ServerEngine.Common
{
    public static class UUIDGenerator
    {
        public static string Get => Guid.NewGuid().ToString();
    }

    public class UIDGenerator
    {
        public enum eGenerateType
        {
            None = 0,
            Max = 1
        }

        private int mServerGid;
        private int mServerIndex;
        private eGenerateType mGenerateType;
        private volatile int mLoopCount = 0;

        private static int mLoopLimit;
        private static int mLoopLimitLength;

        public UIDGenerator(eGenerateType type, int server_gid, int server_index, int loop_limit)
        {
            mServerGid = server_gid;
            mServerIndex = server_index;
            mGenerateType = type;

            mLoopLimit = loop_limit;
            mLoopLimitLength = loop_limit.ToString().Length;
        }

        public string Get()
        {
            int loop_count = Interlocked.Increment(ref mLoopCount) / mLoopLimit;
            int uid_loop_count = loop_count.ToString("D").Length + (mLoopLimitLength - loop_count.ToString().Length);

            var create_time = DateTime.UtcNow.ToUnixTime();

            // ex) 2011 11 10 1724863307 0001 (22자리)
            var uid = $"{mServerGid}{mServerIndex}{(int)mGenerateType}{create_time}{uid_loop_count}";
            return uid;
        }
    }
}
