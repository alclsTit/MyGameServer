using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Sinks.SystemConsole.Themes;
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

        public ILogger GetLogger(string path, string? name, Config.Logger? config = default, SeriLogger.eOutputFormat output_format = SeriLogger.eOutputFormat.Text);
    }
}
