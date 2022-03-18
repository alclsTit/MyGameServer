using System;
using System.Reflection;

namespace ServerEngine.Log
{
    /// <summary>
    /// Factory Method 패턴에서 사용할 객체 생성 추상클래스
    /// 해당 포맷을 기본으로 제공하는 로거 클래스(콘솔, 텍스트, DB 등...) 구현
    /// [Error] level 이상의 로그들은 무조건 로그 저장, 그 이하(debug, warn, info)는 enable 체크를 하여 저장
    /// </summary>
    public abstract class Logger
    {
        public string name { get; private set; }
        protected log4net.ILog ILogger { get; private set; }

        public bool IsDebugEnabled => ILogger.IsDebugEnabled;

        public bool IsInfoEnabled => ILogger.IsInfoEnabled;

        public bool IsWarnEnabled => ILogger.IsWarnEnabled;

        public bool IsErrorEnabled => ILogger.IsErrorEnabled;

        public bool IsFatalEnabled => ILogger.IsFatalEnabled;

        protected Logger(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            this.name = name;
            ILogger = log4net.LogManager.GetLogger(name);
        }

        public virtual void Debug(object message) { ILogger.Debug(message); }

        public virtual void Debug(object message, Exception exception) { ILogger.Debug(message, exception); }

        public virtual void DebugFormat(string format, params object[] args) { ILogger.DebugFormat(format, args); }

        public virtual void DebugFormat(string format, object arg0) { ILogger.DebugFormat(format, arg0); }

        public virtual void DebugFormat(string format, object arg0, object arg1) { ILogger.DebugFormat(format, arg0, arg1); }

        public virtual void DebugFormat(string format, object arg0, object arg1, object arg2) { ILogger.DebugFormat(format, arg0, arg1, arg2); }

        public virtual void DebugFormat(IFormatProvider provider, string format, params object[] args) { ILogger.DebugFormat(provider, format, args); }

        public virtual void Info(object message) { ILogger.Info(message); }

        public virtual void Info(object message, Exception exception) { ILogger.Info(message, exception); }

        public virtual void InfoFormat(string format, params object[] args) { ILogger.InfoFormat(format, args); }

        public virtual void InfoFormat(string format, object arg0) { ILogger.InfoFormat(format, arg0); }

        public virtual void InfoFormat(string format, object arg0, object arg1) { ILogger.InfoFormat(format, arg0, arg1); }

        public virtual void InfoFormat(string format, object arg0, object arg1, object arg2) { ILogger.InfoFormat(format, arg0, arg1, arg2); }

        public virtual void InfoFormat(IFormatProvider provider, string format, params object[] args) { ILogger.InfoFormat(provider, format, args); }

        public virtual void Warn(object message) { ILogger.Warn(message); }

        public virtual void Warn(object message, Exception exception) { ILogger.Warn(message, exception); }

        public virtual void WarnFormat(string format, params object[] args) { ILogger.WarnFormat(format, args); }

        public virtual void WarnFormat(string format, object arg0) { ILogger.WarnFormat(format, arg0); }

        public virtual void WarnFormat(string format, object arg0, object arg1) { ILogger.WarnFormat(format, arg0, arg1); }

        public virtual void WarnFormat(string format, object arg0, object arg1, object arg2) { ILogger.WarnFormat(format, arg0, arg1, arg2); }

        public virtual void WarnFormat(IFormatProvider provider, string format, params object[] args) { ILogger.WarnFormat(provider, format, args); }

        public virtual void Error(object message) { ILogger.Error(message); }

        public virtual void Error(object message, Exception exception) { ILogger.Error(message, exception); }

        public virtual void ErrorFormat(string format, params object[] args) { ILogger.ErrorFormat(format, args); }

        public virtual void ErrorFormat(string format, object arg0) { ILogger.ErrorFormat(format, arg0); }

        public virtual void ErrorFormat(string format, object arg0, object arg1) { ILogger.ErrorFormat(format, arg0, arg1); }

        public virtual void ErrorFormat(string format, object arg0, object arg1, object arg2) { ILogger.ErrorFormat(format, arg0, arg1, arg2); }

        public virtual void ErrorFormat(IFormatProvider provider, string format, params object[] args) { ILogger.ErrorFormat(provider, format, args); }

        public virtual void Fatal(object message) { ILogger.Fatal(message); }

        public virtual void Fatal(object message, Exception exception) { ILogger.Fatal(message, exception); }

        public virtual void FatalFormat(string format, params object[] args) { ILogger.FatalFormat(format, args); }

        public virtual void FatalFormat(string format, object arg0) { ILogger.FatalFormat(format, arg0); }

        public virtual void FatalFormat(string format, object arg0, object arg1) { ILogger.FatalFormat(format, arg0, arg1); }

        public virtual void FatalFormat(string format, object arg0, object arg1, object arg2) { ILogger.FatalFormat(format, arg0, arg1, arg2); }

        public virtual void FatalFormat(IFormatProvider provider, string format, params object[] args) { ILogger.FatalFormat(provider, format, args); }

        public virtual void Debug(string nameOfClass, string nameOfMethod, string message = "")
        {
            ILogger.Warn($"Debug in {nameOfClass}.{nameOfMethod} - {message}");
        }

        public virtual void Debug(string nameOfClass, string nameOfMethod, Exception exception, string message = "")
        {
            ILogger.Warn($"Debug in {nameOfClass}.{nameOfMethod} - {message}", exception);
        }

        public virtual void Info(string nameOfClass, string nameOfMethod, string message = "")
        {
            ILogger.Warn($"Info in {nameOfClass}.{nameOfMethod} - {message}");
        }

        public virtual void Info(string nameOfClass, string nameOfMethod, Exception exception, string message = "")
        {
            ILogger.Warn($"Info in {nameOfClass}.{nameOfMethod} - {message}", exception);
        }

        public virtual void Warn(string nameOfClass, string nameOfMethod, string message = "") 
        {
            ILogger.Warn($"Warning in {nameOfClass}.{nameOfMethod} - {message}");
        }

        public virtual void Warn(string nameOfClass, string nameOfMethod, Exception exception, string message = "") 
        {
            ILogger.Warn($"Warning in {nameOfClass}.{nameOfMethod} - {message}", exception);
        }

        public virtual void Error(string nameOfClass, string nameOfMethod, string message = "")
        {
            ILogger.Error($"Exception[Error] in {nameOfClass}.{nameOfMethod} - {message}");
        }

        public virtual void Error(string nameOfClass, string nameOfMethod, Exception exception, string message = "")
        {
            ILogger.Error($"Exception[Error] in {nameOfClass}.{nameOfMethod} - {message}", exception);
        }

        public virtual void Fatal(string nameOfClass, string nameOfMethod, string message = "")
        {
            ILogger.Fatal($"Exception[Fatal] in {nameOfClass}.{nameOfMethod} - {message}");
        }

        public virtual void Fatal(string nameOfClass, string nameOfMethod, Exception exception, string message = "")
        {
            ILogger.Fatal($"Exception[Fatal] in {nameOfClass}.{nameOfMethod} - {message}", exception);
        }

    }
}
