using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Log
{
    /// <summary>
    /// LogFactory 객체 생성 구현 파생클래스 
    /// </summary>
    public class ConsoleLoggerFactory : ILogFactory
    {
        public Logger GetLogger(string name, string nameOfConsoleTitle)
        {
            return new ConsoleLogger(name, nameOfConsoleTitle);
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
        public ConsoleLogger(String name, string nameOfConsoleTitle) : base(name)
        {
            if (!string.IsNullOrEmpty(nameOfConsoleTitle))
                Console.Title = nameOfConsoleTitle; 
        }

        public override void Debug(object message)
        {
            base.Debug(message);
        }

        public override void Debug(object message, Exception exception)
        {
            base.Debug(message, exception);
        }

        public override void Info(object message)
        {
            base.Info(message);
        }

        public override void Info(object message, Exception exception)
        {
            base.Info(message, exception);
        }

        public override void Warn(object message)
        {
            base.Warn(message);
        }

        public override void Warn(object message, Exception exception)
        {
            base.Warn(message, exception);
        }

        public override void Error(object message)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Error(message);

            Console.ResetColor();
        }

        public override void Error(object message, Exception exception)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Error(message, exception);

            Console.ResetColor();
        }

        public override void Error(string nameOfClass, string nameOfMethod, string message = "")
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Error(nameOfClass, nameOfMethod, message);

            Console.ResetColor();
        }

        public override void Error(string nameOfClass, string nameOfMethod, Exception exception, string message = "")
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Error(nameOfClass, nameOfMethod, exception, message);

            Console.ResetColor();
        }

        public override void Fatal(object message)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Fatal(message);

            Console.ResetColor();
        }

        public override void Fatal(object message, Exception exception)
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Fatal(message, exception);

            Console.ResetColor();
        }

        public override void Fatal(string nameOfClass, string nameOfMethod, string message = "")
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Fatal(nameOfClass, nameOfMethod, message);

            Console.ResetColor();
        }

        public override void Fatal(string nameOfClass, string nameOfMethod, Exception exception, string message = "")
        {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Red;

            base.Fatal(nameOfClass, nameOfMethod, exception, message);

            Console.ResetColor();
        }
    }
}
