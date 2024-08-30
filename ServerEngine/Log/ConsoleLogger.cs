using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerEngine.Config;

namespace ServerEngine.Log
{
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
           return new ConsoleLogger(name);
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

    /// <summary>
    /// Console 창에 찍히는 로그 관련 클래스
    /// LogFactory 디자인 패턴에의해 생성되는 실제 객체
    /// 외부에서 ILog.IsXXXEnabled 체크하는 것이 크진 않더라도 성능상 유리 
    ///  * 오류메시지 생성시 string을 여러개 이어붙일 때 성능 이슈가 발생할 수 있는데, 이를 사전에 차단할 수 있다
    /// </summary>
    public sealed class ConsoleLogger : Logger
    {
        public ConsoleLogger(string name) : base(name) { }

        #region Debug
        public override void Debug(string message, System.Exception? exception = default)
        {
            base.Debug(message, exception);
        }
        public override void Debug(string message, string class_name, string method_name, Exception? exception = default)
        {
            base.Debug(message, class_name, method_name, exception);
        }
        #endregion

        #region Info
        public override void Info(string message, System.Exception? exception = default)
        {
            base.Info(message, exception);
        }
        public override void Info(string message, string class_name, string method_name, Exception? exception = default)
        {
            base.Info(message, class_name, method_name, exception);
        }
        #endregion

        #region Warn
        public override void Warn(string message, Exception? exception = default)
        {
            base.Warn(message, exception);
        }

        public override void Warn(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            base.Warn(message, class_name, method_name, exception);
        }
        #endregion

        #region Error
        public override void Error(string message, Exception? exception = default)
        {
            base.Error(message, exception);
        }
        public override void Error(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Error(message, class_name, method_name, exception);

            Console.ResetColor();
        }
        #endregion

        #region Fatal
        public override void Fatal(string message, Exception? exception = default)
        {
            base.Fatal(message, exception);
        }
        public override void Fatal(string message, string class_name, string method_name, System.Exception? exception = default)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Fatal(message, class_name, method_name, exception);

            Console.ResetColor();
        }
        #endregion
    }


    public sealed class ConsoleFileLogger : SeriLogger
    {
        private bool mDisposed = false;

        public ConsoleFileLogger(string path, string? name = default, Config.ILogger? config = default, eOutputFormat output_format = eOutputFormat.Text) 
            : base(path, name, config, output_format) 
        { 
        }

        #region Debug
        public override void Debug(string message, Exception? exception = default)
        {
            base.Debug(message, exception);
        }
        public override void Debug(string message, string class_name, string method_name, Exception? exception = default)
        {
            base.Debug(message, class_name, method_name, exception);
        }
        #endregion

        #region Info
        public override void Info(string message, Exception? exception = default)
        {
            base.Info(message, exception);
        }
        public override void Info(string message, string class_name, string method_name, Exception? exception = default)
        {
            base.Info(message, class_name, method_name, exception);
        }
        #endregion

        #region Warn
        public override void Warn(string message, Exception? exception = default)
        {
            base.Warn(message, exception);
        }
        public override void Warn(string message, string class_name, string method_name, Exception? exception = default)
        {
            base.Warn(message, class_name, method_name, exception);
        }
        #endregion

        #region Error
        public override void Error(string message, Exception? exception = default)
        {
            base.Error(message, exception);
        }
        public override void Error(string message, string class_name, string method_name, Exception? exception = default)
        {
            base.Error(message, class_name, method_name, exception);
        }
        #endregion

        #region Fatal
        public override void Fatal(string message, Exception? exception = default)
        {
            base.Fatal(message, exception);
        }
        public override void Fatal(string message, string class_name, string method_name, Exception? exception = default)
        {
            base.Fatal(message, class_name, method_name, exception);
        }
        #endregion

        public override void Dispose()
        {
            if (mDisposed)
                return;

            mDisposed = true;
            
            base.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            if (mDisposed)
                return default;

            mDisposed = true;
            
            return base.DisposeAsync();   
        }
    }
}
