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
        public static int ToUnixTime(this DateTime cur_datetime)
        {
            TimeSpan interval = cur_datetime - DateTime.UnixEpoch;
            return (int)interval.TotalSeconds;
        }

        #endregion
    }
}
