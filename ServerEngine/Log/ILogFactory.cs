using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Serilog.Sinks.SystemConsole.Themes;
using ServerEngine.Config;
using static ServerEngine.Log.LoggerFactory;

namespace ServerEngine.Log
{
    public interface ILogger
    {
        public bool IsEnableDebug { get; }
        public bool IsEnableInfo { get; }
        public bool IsEnableWarn { get; }
        public bool IsEnableError { get; }
        public bool IsEnableFatal { get; }

        public void Debug(string message, System.Exception? exception = default);
        public void Info(string message, System.Exception? exception = default);
        public void Warn(string message, System.Exception? exception = default);
        public void Error(string message, System.Exception? exception = default);
        public void Fatal(string message, System.Exception? exception = default);

        public void Debug(string message, string class_name, string method_name, System.Exception? exception = default);
        public void Info(string message, string class_name, string method_name, System.Exception? exception = default);
        public void Warn(string message, string class_name, string method_name, System.Exception? exception = default);
        public void Error(string message, string class_name, string method_name, System.Exception? exception = default);
        public void Fatal(string message, string class_name, string method_name, System.Exception? exception = default);
    }

    /// <summary>
    /// Factory Method Design Pattern
    /// </summary>
    public interface ILogFactory
    {
        public ILogger GetLogger(string name);

        public ILogger GetLogger(string path, string? name, Config.ILogger? config = default, SeriLogger.eOutputFormat output_format = SeriLogger.eOutputFormat.Text);
    }

    /// <summary>
    /// LogFactory 객체 생성 구현 파생클래스 
    /// </summary>
    public class LoggerFactory : ILogFactory
    {
        /// <summary>
        /// Log4Net을 사용한 Logger
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public ILogger GetLogger(string name)
        {
            return new ConcreteLogger(name);
        }

        /// <summary>
        /// SeriLog를 사용한 Logger
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="output_format"></param>
        /// <returns></returns>

        public ILogger GetLogger(string path, string? name, Config.ILogger? config = default, SeriLogger.eOutputFormat output_format = SeriLogger.eOutputFormat.Text)
        {
            var root_path = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName);
            var log_path = Path.Join(root_path, "logs");

            var config_etc = ConfigLoader.LoadJson<IConfigEtc>("config_etc", ConfigLoader.eFileExtensionType.json);
            if (null != config_etc)
            {
                return new ConsoleFileLogger(log_path, config_etc.name, config_etc.logger, output_format);
            }
            else
            {
                return new ConsoleFileLogger(log_path);
            }
        }
    }
}
