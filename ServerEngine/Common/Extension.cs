using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ServerEngine.Common
{
    public static class Extension
    {
        public static string ClassName(this object item) 
        {
            return item.GetType().Name;
        }

        public static string? MethodName(this object item) 
        {
            var calledFuncName = new System.Diagnostics.StackFrame(1).GetMethod()?.Name;
            return calledFuncName;
        }

        public static void DoNotWait(this Task task)
        {
        }

        #region TimeExtension
        public static uint ToUnixTimeUInt32(this DateTime datetime)
        {
            TimeSpan interval = datetime - DateTime.UnixEpoch;
            return (uint)interval.TotalSeconds;
        }

        public static ulong ToUnixTimeUInt64(this DateTime datetime)
        {
            TimeSpan interval = datetime - DateTime.UnixEpoch;
            return (ulong)interval.TotalSeconds;
        }
        #endregion
    }
}
