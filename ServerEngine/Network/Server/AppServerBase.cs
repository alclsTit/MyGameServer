using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Log;
using ServerEngine.Config;
using ServerEngine.Network.SystemLib;
using ServerEngine.Common;

namespace ServerEngine.Network.Server
{
    /*public abstract class AppServerBase : IAppServer
    {
        //private static bool msInitConfigLoaderFlag = false;

        //private static bool msInitThreadPoolFlag = false;

        /// <summary>
        /// logger 관련 logFactory(생성자) 및 logger(구체적 생성 객체)
        /// </summary>
        public ILogFactory logFactory { get; private set; }
        public Logger logger { get; private set; }

        /// <summary>
        /// 서버 옵션 세팅
        /// </summary>
        //private IServerInfo mServerConfig;

        /// <summary>
        /// 텍스트 인코딩 방식 지정 (default = ANSI)
        /// </summary>
        public Encoding mTextEncodig { get; private set; }

        /// <summary>
        /// 서버 Listen 관련 옵션 세팅
        /// </summary>
        protected List<IListenInfo> mListenInfos { get; private set; } = new List<IListenInfo>();

        /// <summary>
        /// TCP Listener 관리자
        /// </summary>
        public List<TCPListener> mTCPListeners { get; private set; }

        /// <summary>
        /// 서버 상태
        /// </summary>
        public int state { get; protected set; } = ServerState.NotInitialized;

        /// <summary>
        /// 서버 이름
        /// </summary>
        public string name { get; private set; }

        /// <summary>
        /// 서버 응용프로그램 위에서 작동되는 여러 서버모듈(클라이언트와 실제 통신 진행)
        /// </summary>
        protected List<IServerModule> mServerModuleList;

        protected AppServerBase(ILogFactory logFactory = null)
        {
            // Logger는 null이면 디폴트로 ConsoleLogger가 생성되도록 한다
            this.logFactory = logFactory ?? new ConsoleLoggerFactory();
            logger = this.logFactory.GetLogger(nameof(AppServerBase), "ServerEngine");
        }
        
        /// <summary>
        /// 어플리케이션 구동에 필요한 Setup 작업은 파생클래스에서 사용자가 커스터마이징해서 사용
        /// </summary>
        public abstract void Setup();

        /// <summary>
        /// 어플리케이션 구동에 필요한 Start 작업은 파생클래스에서 사용자가 커스터마이징해서 사용
        /// </summary>

        public abstract void Start();


        /// <summary>
        /// 어플리케이션 구동에 필요한 Connect 작업은 파생클래스에서 사용자가 커스터마이징해서 사용
        /// </summary>
        public abstract void StartConnect();


        #region "INITIALIZE"
        /// <summary>
        /// Server 구성 세팅 - Config 파일로드 이전에 미리 세팅해둘 수 있는 대상들 세팅
        /// </summary>
        /// <param name="configFileName"></param>
        
        public virtual void Initialize(string configFileName, eFileExtension fileExtension = eFileExtension.INI)
        {
            if (msInitConfigLoaderFlag == false)
            {
                ConfigLoader.Instance.Initialize(configFileName, fileExtension);
                msInitConfigLoaderFlag = true;
            }
            
        }
        
        #endregion

        #region "SERVER SETUP"
        /// <summary>
        /// Server 구성 세팅 - Config 파일로드 이후 세팅할 수 있는 대상들 세팅
        /// </summary>
        /// <returns></returns>
        protected virtual bool SetupBase()
        {
            if (!ConfigLoader.Instance.LoadConfig(mListenInfos, out mServerConfig))
            {
                logger.Error(this.ClassName(), this.MethodName(), "Fail to load config file");
                return false;
            }

            name = mServerConfig.serverName;

            mTextEncodig = string.IsNullOrEmpty(mServerConfig.encoding) ? Encoding.Default : Encoding.GetEncoding(mServerConfig.encoding);

            if (!msInitThreadPoolFlag)
            {
                if (ThreadPoolEx.ResetThreadPoolInfo(mServerConfig.minWorkThreadCount,
                                                     mServerConfig.maxWorkThreadCount,
                                                     mServerConfig.minIOThreadCount,
                                                     mServerConfig.maxIOThreadCount))
                {
                    msInitThreadPoolFlag = true;
                }

            }
            

            return true;
        }

        /// <summary>
        /// Server 구성 세팅 - AppServer 위에서 작동되는 개별 서버모듈 세팅
        /// </summary>
        /// <returns></returns>
        protected virtual bool SetupSocketServer()
        {
            mServerModuleList = mServerModuleList ?? new List<IServerModule>();
 
            foreach(var tcpListener in mTCPListeners)
            {
                var serverModule = new ServerAcceptModule();
                serverModule.Initialize(tcpListener, mServerConfig, logger);
                mServerModuleList.Add(serverModule);
            }

            return true;
        }

        #endregion

        #region "LISTENER SETTING"
        /// <summary>
        /// 현재 구동중인 Listeners에 새롭게 추가하려는 Listener 정보가 이미 존재하는 경우 체크 
        /// </summary>
        /// <param name="listenInfo"></param>
        /// <returns></returns>
        private bool CheckAlreadyHaveListener(IListenInfo listenInfo)
        {
            return mTCPListeners.Exists(oldListener => oldListener.GetIPEndPoint.Address.ToString().Equals(listenInfo.ip, StringComparison.OrdinalIgnoreCase) &&
                                                       oldListener.GetIPEndPoint.Port == listenInfo.port);
        }

        /// <summary>
        /// Server Listener 세팅. 모두 정상적으로 세팅되지 않으면 서버 시작되지 않도록 설정
        /// </summary>
        /// <returns></returns>
        protected virtual bool SetupListeners()
        {
            mTCPListeners = mTCPListeners ?? new List<TCPListener>();

            foreach (var listenInfo in mListenInfos)
            {
                if (string.IsNullOrEmpty(listenInfo.ip))
                    throw new ArgumentException($"[{listenInfo.serverName}]_IP is null");

                if (listenInfo.port <= 0)
                    throw new Exception($"[{listenInfo.serverName}]_PORT is zero under");

                // 현재 추가하려는 Listener가 이미 존재하는 경우
                if (CheckAlreadyHaveListener(listenInfo))
                    continue;

                var newListener = new TCPListener(listenInfo, mServerConfig, this, () => new ServerSession.Session());
                mTCPListeners.Add(newListener);
            }

            return true;
        }
        
        #endregion

        

        #region "SERVER START"
        protected virtual void StartServerModule()
        {
            foreach(var module in mServerModuleList)
            {
                module.Start();
            }
        }
        
        #endregion

        #region "CLIENT START"

        protected virtual void StartClientModule()
        {
            foreach(var module in mServerModuleList)
            {
                module.StartConnect();
            }
        }
        #endregion
    }
    */
}
