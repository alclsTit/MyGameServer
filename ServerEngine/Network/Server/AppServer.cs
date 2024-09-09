using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using ServerEngine.Log;
using ServerEngine.Common;

namespace ServerEngine.Network.Server
{
    // 24.09.09 삭제 예정
    /*public sealed class AppServer : AppServerBase
    {
        private ThreadManager mThreadManager;

        public AppServer(ILogFactory logFactory) : base(logFactory) { }

        /// <summary>
        /// 외부에서 단독 호출
        /// </summary>
        /// <param name="configFileName"></param>
        /// <param name="fileExtension"></param>
        public override void Initialize(string configFileName, eFileExtension fileExtension = eFileExtension.INI)
        {
            try
            {
                //base.Initialize(configFileName, fileExtension);

                state = ServerState.Initialized;

                if (logger.IsDebugEnabled)
                    logger.Debug("AppServer [Initialize] finished!!!");
            }
            catch (ArgumentNullException argNullEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), argNullEx);
            }
            catch (System.IO.DirectoryNotFoundException dirException)
            {
                logger.Error(this.ClassName(), this.MethodName(), dirException);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }

        /// <summary>
        /// 외부에서 단독 호출
        /// </summary>
        public override void Setup()
        {
            try
            {
                if (!SetupBase())
                {
                    logger.Error(this.ClassName(), this.MethodName(), "Fail to set SetupBase Function");
                    return;
                }

                if (!SetupListeners())
                {
                    logger.Error(this.ClassName(), this.MethodName(), "Fail to set SetupListeners Function");
                    return;
                }

                if (!SetupSocketServer())
                {
                    logger.Error(this.ClassName(), this.MethodName(), "Fail to set SetupSocketServer Function");
                    return;
                }

                state = ServerState.SetupFinished;

                mThreadManager = mThreadManager ?? new ThreadManager();

                if (logger.IsDebugEnabled)
                    logger.Debug("AppServer [Setup] finished!!!");
            }
            catch (ArgumentNullException argNullEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), argNullEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }

        /// <summary>
        /// 외부에서 단독 호출 
        /// </summary>
        public override void Start()
        {
            try
            {
                if (state != ServerState.SetupFinished)
                {
                    logger.Error(this.ClassName(), this.MethodName(), "[AppServer[Start] state isn't [SetupFinished] before starting");
                    return;
                }

                var result = mThreadManager.TryAddThread("ServerModuleThread", StartServerModule);
                if (!result.Result)
                {
                    logger.Error(this.ClassName(), this.MethodName(), "Fail to start [StartServerModule]");
                    return;
                }

                mThreadManager.StartThreads().DoNotWait();
                
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }

        public override void StartConnect()
        {
            try
            {
                if (state != ServerState.SetupFinished)
                {
                    logger.Error(this.ClassName(), this.MethodName(), "AppServer[StartConnect] state isn't [SetupFinished] before starting");
                    return;
                }

                var result = mThreadManager.TryAddThread("ConnectModuleThread", StartClientModule);
                if (!result.Result)
                {
                    logger.Error(this.ClassName(), this.MethodName(), "Fail to start [ConnectModuleThread]");
                    return;
                }

                mThreadManager.StartThreads().DoNotWait();
                
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }
    }
*/
}
