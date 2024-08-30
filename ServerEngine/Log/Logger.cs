using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using System.Reflection;
using Serilog;
using Serilog.Core;
using Serilog.Data;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ServerEngine.Config;

namespace ServerEngine.Log
{
    /// <summary>
    /// Factory Method 패턴에서 사용할 객체 생성 추상클래스
    /// 해당 포맷을 기본으로 제공하는 로거 클래스(콘솔, 텍스트, DB 등...) 구현
    /// [Error] level 이상의 로그들은 무조건 로그 저장, 그 이하(debug, warn, info)는 enable 체크를 하여 저장
    /// </summary>
    public abstract class Logger : ILogger
    {
        private readonly log4net.ILog ILogger;
        
        public string name { get; private set; }

        public bool IsEnableDebug => ILogger.IsDebugEnabled;
        public bool IsEnableInfo => ILogger.IsInfoEnabled;
        public bool IsEnableWarn => ILogger.IsWarnEnabled;
        public bool IsEnableError => ILogger.IsErrorEnabled;
        public bool IsEnableFatal => ILogger.IsFatalEnabled;

        protected Logger(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            this.name = name;
            ILogger = log4net.LogManager.GetLogger(name);
        }

        //public virtual void Debug(object message) { ILogger.Debug(message); }

        public virtual void Debug(string message, System.Exception? exception = default) 
        {
            if (default == exception) ILogger.Debug(message);
            else ILogger.Debug(message, exception);
        }
        public virtual void Debug(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            var _message = $"[{class_name}.{method_name}()] - {message}";

            if (default == exception) ILogger.Debug(message);
            else ILogger.Debug(message, exception);
        }

        //public virtual void DebugFormat(string format, params object[] args) { ILogger.DebugFormat(format, args); }

        //public virtual void DebugFormat(string format, object arg0) { ILogger.DebugFormat(format, arg0); }

        //public virtual void DebugFormat(string format, object arg0, object arg1) { ILogger.DebugFormat(format, arg0, arg1); }

        //public virtual void DebugFormat(string format, object arg0, object arg1, object arg2) { ILogger.DebugFormat(format, arg0, arg1, arg2); }

        //public virtual void DebugFormat(IFormatProvider provider, string format, params object[] args) { ILogger.DebugFormat(provider, format, args); }

        //public virtual void Info(object message) { ILogger.Info(message); }

        public virtual void Info(string message, System.Exception? exception = default) 
        {
            if (default == exception) ILogger.Info(message);
            else ILogger.Info(message, exception);
        }
        public virtual void Info(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            var _message = $"[{class_name}.{method_name}()] - {message}";

            if (default == exception) ILogger.Info(_message);
            else ILogger.Info(_message, exception);
        }

        //public virtual void InfoFormat(string format, params object[] args) { ILogger.InfoFormat(format, args); }

        //public virtual void InfoFormat(string format, object arg0) { ILogger.InfoFormat(format, arg0); }

        //public virtual void InfoFormat(string format, object arg0, object arg1) { ILogger.InfoFormat(format, arg0, arg1); }

        //public virtual void InfoFormat(string format, object arg0, object arg1, object arg2) { ILogger.InfoFormat(format, arg0, arg1, arg2); }

        //public virtual void InfoFormat(IFormatProvider provider, string format, params object[] args) { ILogger.InfoFormat(provider, format, args); }

        //public virtual void Warn(object message) { ILogger.Warn(message); }

        public virtual void Warn(string message, System.Exception? exception = default) 
        {
            if (default == exception) ILogger.Warn(message);
            else ILogger.Warn(message, exception); 
        }
        public virtual void Warn(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            var _message = $"[{class_name}.{method_name}()] - {message}";
            
            if (default == exception) ILogger.Warn(_message);
            else ILogger.Warn(_message, exception);
        }

        //public virtual void WarnFormat(string format, params object[] args) { ILogger.WarnFormat(format, args); }

        //public virtual void WarnFormat(string format, object arg0) { ILogger.WarnFormat(format, arg0); }

        //public virtual void WarnFormat(string format, object arg0, object arg1) { ILogger.WarnFormat(format, arg0, arg1); }

        //public virtual void WarnFormat(string format, object arg0, object arg1, object arg2) { ILogger.WarnFormat(format, arg0, arg1, arg2); }

        //public virtual void WarnFormat(IFormatProvider provider, string format, params object[] args) { ILogger.WarnFormat(provider, format, args); }

        //public virtual void Error(object message) { ILogger.Error(message); }

        public virtual void Error(string message, System.Exception? exception = default) 
        {
            if (default == exception) ILogger.Error(message);
            else ILogger.Error(message, exception); 
        }
        public virtual void Error(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            var _message = $"[{class_name}.{method_name}()] - {message}";

            if (default == exception) ILogger.Fatal(_message);
            else ILogger.Fatal(_message, exception);
        }

        //public virtual void ErrorFormat(string format, params object[] args) { ILogger.ErrorFormat(format, args); }

        //public virtual void ErrorFormat(string format, object arg0) { ILogger.ErrorFormat(format, arg0); }

        //public virtual void ErrorFormat(string format, object arg0, object arg1) { ILogger.ErrorFormat(format, arg0, arg1); }

        //public virtual void ErrorFormat(string format, object arg0, object arg1, object arg2) { ILogger.ErrorFormat(format, arg0, arg1, arg2); }

        //public virtual void ErrorFormat(IFormatProvider provider, string format, params object[] args) { ILogger.ErrorFormat(provider, format, args); }

        //public virtual void Fatal(object message) { ILogger.Fatal(message); }
        public virtual void Fatal(string message, System.Exception? exception = default)
        {
            if (default == exception) ILogger.Fatal(message);
            else ILogger.Fatal(message, exception);
        }

        public virtual void Fatal(string message, string class_name, string method_name,  System.Exception? exception = default) 
        {
            var _message = $"[{class_name}.{method_name}()] - {message}";

            if (default == exception) ILogger.Fatal(_message);    
            else ILogger.Fatal(_message, exception); 
        }

        //public virtual void FatalFormat(string format, params object[] args) { ILogger.FatalFormat(format, args); }

        //public virtual void FatalFormat(string format, object arg0) { ILogger.FatalFormat(format, arg0); }

        //public virtual void FatalFormat(string format, object arg0, object arg1) { ILogger.FatalFormat(format, arg0, arg1); }

        //public virtual void FatalFormat(string format, object arg0, object arg1, object arg2) { ILogger.FatalFormat(format, arg0, arg1, arg2); }

        //public virtual void FatalFormat(IFormatProvider provider, string format, params object[] args) { ILogger.FatalFormat(provider, format, args); }
    }


    public abstract class SeriLogger : ILogger
    {
        public enum eOutputFormat
        {
            Text = 0,
            Json = 1
        }

        private readonly Serilog.Core.Logger Logger;
        
        public string? name { get; private set; }

        public bool IsEnableDebug => Logger.IsEnabled(LogEventLevel.Debug);
        public bool IsEnableInfo => Logger.IsEnabled(LogEventLevel.Information);
        public bool IsEnableWarn => Logger.IsEnabled(LogEventLevel.Warning);
        public bool IsEnableError => Logger.IsEnabled(LogEventLevel.Error);
        public bool IsEnableFatal => Logger.IsEnabled(LogEventLevel.Fatal);

        #region SeriLogEnrich
        class ThreadIdEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ThreadId", Thread.CurrentThread.ManagedThreadId));
            }
        }

        class ProcessIdEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ProcessId", System.Diagnostics.Process.GetCurrentProcess().Id));
            }
        }

        class ProcessNameEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                    "ProcessName", System.Diagnostics.Process.GetCurrentProcess().ProcessName));
            }
        }
        #endregion

        public SeriLogger(string path, string? name = default, Config.ILogger? config = default, eOutputFormat output_format = eOutputFormat.Text)
        {
            var output_template = "[{Timestamp:yyyy-MM-dd HH:mm:ss:ff}][{Level:u15}][{ProcessName}_{ProcessId}][{ThreadId}] {Message:lj}{NewLine}{Exception}";

            if (default != config && false == string.IsNullOrEmpty(name) && true == GetConfig(name, config, out var config_data))
            {
                if (eOutputFormat.Text == output_format)
                {
                    this.Logger = new LoggerConfiguration().
                        Enrich.With(new ProcessNameEnricher()).
                        Enrich.With(new ProcessIdEnricher()).
                        Enrich.With(new ThreadIdEnricher()).
                        WriteTo.File(path,
                                     levelSwitch: new LoggingLevelSwitch(initialMinimumLevel: (LogEventLevel)config_data.file_minlevel),
                                     rollingInterval: (RollingInterval)config_data.rolling_interval,
                                     outputTemplate: output_template).
                        WriteTo.Console(levelSwitch: new LoggingLevelSwitch(initialMinimumLevel: (LogEventLevel)config_data.console_minlevel),
                                        outputTemplate: output_template,
                                        theme: AnsiConsoleTheme.Code).
                        CreateLogger();
                }
                else
                {
                    this.Logger = new LoggerConfiguration().
                       Enrich.With(new ProcessNameEnricher()).
                       Enrich.With(new ProcessIdEnricher()).
                       Enrich.With(new ThreadIdEnricher()).
                       WriteTo.File(new Serilog.Formatting.Compact.CompactJsonFormatter(),
                                    path,
                                    levelSwitch: new LoggingLevelSwitch(initialMinimumLevel: (LogEventLevel)config_data.file_minlevel),
                                    rollingInterval: (RollingInterval)config_data.rolling_interval).
                       WriteTo.Console(levelSwitch: new LoggingLevelSwitch(initialMinimumLevel: (LogEventLevel)config_data.console_minlevel),
                                       outputTemplate: output_template,
                                       theme: AnsiConsoleTheme.Code).
                       CreateLogger();
                }
            }
            else
            {
                this.Logger = new LoggerConfiguration().
                    Enrich.With(new ProcessNameEnricher()).
                    Enrich.With(new ProcessIdEnricher()).
                    Enrich.With(new ThreadIdEnricher()).
                    WriteTo.File(path,
                                 restrictedToMinimumLevel: LogEventLevel.Verbose,
                                 rollingInterval: RollingInterval.Minute,
                                 outputTemplate: output_template).
                    WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Verbose,
                                    outputTemplate: output_template,
                                    theme: AnsiConsoleTheme.Code).
                    CreateLogger();
            }
        }

        /// <summary>
        /// config 파일로부터 logger에 필요한 데이터를 가져온다
        /// </summary>
        /// <param name="name">config 파일내 가져올 옵션이름</param>
        /// <param name="config">타겟 config 파일이 포함된 logger config</param>
        /// <param name="config_data">타겟 config 파일. 메서드가 true인경우 config_data는 null이 아니다. 반환값이 true면 null검사를 수행하지 않는다</param>
        /// <returns></returns>
        private bool GetConfig(string name, Config.ILogger config, [NotNullWhen(true)] out IConfigLog? config_data)
        {
            var _config_data = config.list.Find(e => e.name == name.ToLower().Trim());
            if (default != _config_data)
            {
                config_data = _config_data;
                return true;
            }
            else
            {
                config_data = default;
                return false;
            }
        }

        #region LoggingMethod
        public virtual void Debug(string message, System.Exception? exception = default)
        {
            Logger.Debug(message, exception);
        }
        public virtual void Debug(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            Logger.Debug($"[{class_name}.{method_name}()] - {message}", exception);
        }
        public virtual void Info(string message, System.Exception? exception = default)
        {
            Logger.Information(message, exception);
        }
        public virtual void Info(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            Logger.Information($"[{class_name}.{method_name}()] - {message}", exception);
        }
        public virtual void Warn(string message, System.Exception? exception = default)
        {
            Logger.Warning(message, exception);
        }
        public virtual void Warn(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            Logger.Warning($"[{class_name}.{method_name}()] - {message}", exception);
        }
        public virtual void Error(string message, System.Exception? exception = default)
        {
            Logger.Error(message, exception);
        }
        public virtual void Error(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            Logger.Error($"[{class_name}.{method_name}()] - {message}", exception);
        }
        public virtual void Fatal(string message, System.Exception? exception = default)
        {
            Logger.Fatal(message, exception);
        }
        public virtual void Fatal(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            Logger.Fatal($"[{class_name}.{method_name}()] - {message}", exception);
        }
        #endregion

        public virtual void Dispose()
        {
            Logger.Dispose();
        }

        public virtual ValueTask DisposeAsync()
        {
            return Logger.DisposeAsync();
        }
    }
}
