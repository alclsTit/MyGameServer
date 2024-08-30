using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;

using ServerEngine.Log;
using ServerEngine.Config;
using ServerEngine.Network.SystemLib;
using ServerEngine.Common;
using ServerEngine.Network.ServerSession;

namespace ServerEngine.Network.Server
{
    /// <summary>
    /// 외부에서 가져온 서버 데이터 세팅, 여러개의 서버모듈이 작동되는 베이스 서버 클래스 
    /// * 사용자가 최대한으로 편리하게 사용할 수 있도록 사용자 접근 인터페이스와 구현부는 분리한다
    /// </summary>
    public abstract class ServerBase : IServerBase
    {
        /// <summary>
        /// ThreadPool 세팅 플래그 (한 번만 진행)
        /// </summary>
        private static bool msInitThreadPoolFlag = false;

        /// <summary>
        /// logger 관련 logFactory(생성자) 및 logger(구체적 생성 객체)
        /// </summary>
        public Log.ILogger Logger { get; private set; }

        /// <summary>
        /// server configuration
        /// </summary>
        public IConfigCommon? Config { get; private set; }

        /// <summary>
        /// 서버 Listen 관련 옵션관리 컨테이너 (여러개일수있다)
        /// </summary>
        public List<IConfigListen> mListenInfoList { get; private set; } = new List<IConfigListen>();

        /// <summary>
        /// 문자 인코딩 방식(utf-8, utf-16...)
        /// </summary>
        public Encoding mTextEncoding { get; protected set; } = Encoding.UTF8;

        /// <summary>
        /// 서버 상태
        /// </summary>
        public int mServerState = ServerState.NotInitialized;

        /// <summary>
        /// 서버 이름(RelayServer, AuthServer...)
        /// </summary>
        public string? Name { get; private set; }

        /// <summary>
        /// 서버 응용프로그램 위에서 작동되는 여러 서버모듈(클라이언트와 실제 통신 진행)
        /// </summary>
        protected List<IServerModule>? mServerModuleList;

        protected ServerBase(ILogger logger)
        {
            this.Logger = logger;
        }

        public virtual bool Initialize(string name, IConfigCommon config)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (null == config)
                throw new ArgumentNullException(nameof(config));

            //.NetCore는 .NetFramework와는 다르게 assemblyinfo 파일을 자동으로 구성.전체 구성은 csproj에서... 별도로 log4net 수동으로 로드진행 
            //var logRepository = log4net.LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            //log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

            //this.logFactory = logFactory ?? new LoggerFactory();
            //logger = this.logFactory.GetLogger(nameof(ServerBase), nameOfConsoleTitle);

            //if (logger == null)
            //    return false;

            this.Name = name;
            this.Config = config;

            Message.PacketProcessorManager.Instance.Initialize(logger: Logger);
            
            return true;
        }

        public bool ChangeState(int oldState, int newState)
        {
            var curState = mServerState;
            if (curState == newState)
                return true;

            if (oldState == Interlocked.Exchange(ref mServerState, newState))
                return true;

            return false;
        }

        /// <summary>
        /// 어플리케이션 구동에 필요한 Setup 작업 중 공통작업 구현, 기타 필요한 것들은 파생클래스에서 구현
        /// </summary>
        /// <typeparam name="TServerModule"></typeparam>
        /// <typeparam name="TServerInfo"></typeparam>
        /// <typeparam name="TNetworkSystem"></typeparam>
        /// <param name="listenInfo"></param>
        /// <param name="config"></param>
        /// <param name="creater"></param>
        /// <returns></returns>
        public virtual bool Setup<TServerModule, TServerInfo, TNetworkSystem>(List<IConfigListen> listen_list, TServerInfo config, Func<ServerSession.Session> creater)
            where TServerModule : ServerModuleBase, new()
            where TServerInfo : ServerConfig, new()
            where TNetworkSystem : NetworkSystemBase, new()
        {
            // 1. Config 파일 세팅
            if (!SetupServerConfig(config))
            {
                Logger.Error("Error in ServerBase.Setup() overload_1 - Fail to setup [SetupServerConfig]!!!");
                return false;
            }

            // 2. Listener 세팅
            if (!SetupListener(listen_list))
            {
                Logger.Error("Error in ServerBase.Setup() overload_1 - Fail to setup [SetupListener]!!!");
                return false;
            }

            // 3. ServerModule 세팅
            if (!SetupSocketServer<TServerModule, TServerInfo, TNetworkSystem>(config, creater))
            {
                Logger.Error("Error in ServerBase.Setup() overload_1 - Fail to setup [SetupSocketServer]!!!");
                return false;
            }

            // 4. 패킷 송수신(Accept, Connect) 관련 Thread 세팅
            if (!SetupThreads())
            {
                Logger.Error("Error in ServerBase.Setup() overload_1 - Fail to setup [SetupThreads]!!!");
                return false;
            }

            // 5. Send 패킷관련 Chunk 값 세팅
            // 2022.05.12 Send 패킷관련 작업 중 필요없는 부분 삭제
            // Message.SendMessageHelper.Initialize(config.sendBufferSize);

            return true;
        }

        /// <summary>
        /// 어플리케이션 구동에 필요한 Setup 작업 중 공통작업 구현, 기타 필요한 것들은 파생클래스에서 구현
        /// </summary>
        /// <typeparam name="TServerModule"></typeparam>
        /// <typeparam name="TServerInfo"></typeparam>
        /// <typeparam name="TNetworkSystem"></typeparam>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="serverName"></param>
        /// <param name="config"></param>
        /// <param name="creater"></param>
        /// <param name="backlog"></param>
        /// <param name="nodelay"></param>
        /// <returns></returns>
        public virtual bool Setup<TServerModule, TServerInfo, TNetworkSystem>(string ip, ushort port, string serverName, TServerInfo config, Func<ServerSession.Session> creater, byte backlog)
            where TServerModule : ServerModuleBase, new()
            where TServerInfo : ServerConfig, new()
            where TNetworkSystem : NetworkSystemBase, new()
        {
            // 1. Config 파일 세팅
            if (!SetupServerConfig(config))
            {
                Logger.Error("Error in ServerBase.Setup() overload_2 - Fail to setup [SetupServerConfig]!!!");
                return false;
            }

            // 2. Listener 세팅
            if (!SetupListener(ip, port, serverName, backlog))
            {
                Logger.Error("Error in ServerBase.Setup() overload_2 - Fail to setup [SetupListener]!!!");
                return false;
            }

            // 3. ServerModule 세팅
            if (!SetupSocketServer<TServerModule, TServerInfo, TNetworkSystem>(config, creater))
            {
                Logger.Error("Error in ServerBase.Setup() overload_2 - Fail to setup [SetupSocketServer]!!!");
                return false;
            }

            // 4. 패킷 송수신(Accept, Connect) 관련 Thread 세팅
            if (!SetupThreads())
            {
                Logger.Error("Error in ServerBase.Setup() overload_2 - Fail to setup [SetupThreads]!!!");
                return false;
            }

            // 5. Send 패킷관련 Chunk 값 세팅
            // 2022.05.12 Send 패킷관련 작업 중 필요없는 부분 삭제
            //Message.SendMessageHelper.Initialize(config.sendBufferSize);

            return true;
        }

        /// <summary>
        /// Setup 작업이후 공통적으로 진행되는 후작업 (상태체크..)
        /// </summary>
        /// <returns></returns>
        protected bool SetupAfterCheck()
        {
            var oldState = ServerState.Initialized;
            if (!ChangeState(oldState, ServerState.SetupFinished))
            {
                Logger.Error($"Error in ServerBase.SetupAfterCheck() - State is [{oldState}]. It can be [SetupFinished] when state is [Initialized]");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 어플리케이션 구동에 필요한 Start 추가 작업은 파생클래스에서 사용자가 커스터마이징해서 사용
        /// * 이곳에서는 상태변경 및 Thread 구동
        /// </summary>
        public virtual void Start()
        {
            mServerState = ServerState.Running;

            ThreadManager.StartThreads();
        }

        /// <summary>
        /// 서버 객체 당 공통적으로 config 옵션 세팅이 필요한 대상 세팅 진행
        /// 모든 서버는 config 파일을 통해서 서버 옵션이 필수적으로 세팅되어야한다(강제 하는게 맞는가?)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool SetupServerConfig<TServerInfo>(TServerInfo config) 
            where TServerInfo : ServerConfig
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            mTextEncoding = string.IsNullOrEmpty(config.encoding) ? Encoding.Default : Encoding.GetEncoding(config.encoding);
            
            if (config.controlThreadPoolCount)
            {
                if (!msInitThreadPoolFlag)
                {
                    if (ThreadPoolEx.ResetThreadPoolInfo(config.minWorkThreadCount,
                                                         config.maxWorkThreadCount,
                                                         config.minIOThreadCount,
                                                         config.maxIOThreadCount))
                    {
                        msInitThreadPoolFlag = true;
                    }
                    else
                    {
                        Logger.Warn("Warning in ServerBase.SetupServerConfig() - Fail to set [ThreadPoolEx.ResetThreadPoolInfo], working default ThreadPool count!!!");
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 현재 IConfigListen 중 에 새롭게 추가하려는 listen 정보가 이미 존재하는 경우 체크 
        /// </summary>
        /// <param name="IConfigListen"></param>
        /// <returns></returns>
        private bool CheckAlreadyHaveListener(IConfigListen listen_config)
        {
            return mListenInfoList.Exists(listener => listener.address.Equals(listen_config.address, StringComparison.OrdinalIgnoreCase) &&
                                                      listener.port == listen_config.port);
        }

        /// <summary>
        /// 외부에서 config 파일을 바탕으로 세팅된 데이터를 가져와 Listener 세팅 작업 진행
        /// </summary>
        /// <param name="listenInfoList"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        private bool SetupListener(List<IConfigListen> listen_list)
        {
            if (null == listen_list || 0 >= listen_list.Count)
                throw new ArgumentNullException(nameof(listen_list));

            foreach (var listener in listen_list)
            {
                if (string.IsNullOrEmpty(listener.address))
                {
                    Logger.Error($"Error in ServerBase.SetupListener() - [{Name}] Listen (IPAddress) is Error!!!");
                    return false;
                }

                if (listener.port <= 0)
                {
                    Logger.Error($"Error in ServerBase.SetupListener() - [{Name} Listen (PORT) is Error!!!");
                    return false;
                }
            }

            mListenInfoList = listen_list;
            return true;
        }

        /// <summary>
        /// 외부에서 config 파일을 바탕으로 세팅된 데이터를 가져와 Listener 세팅 작업 진행
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="name"></param>
        /// <param name="backlog"></param>
        /// <param name="nodelay"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        private bool SetupListener(string address, ushort port, string name, byte backlog)
        {
            if (string.IsNullOrEmpty(address))
                throw new ArgumentNullException(nameof(address));

            if (port <= 0)
                throw new ArgumentException(nameof(port));

            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            IConfigListen listenInfo = new DefaultConfigListen(address, port, backlog);

            mListenInfoList = mListenInfoList ?? new List<IConfigListen>();
            if (mListenInfoList.Count > 0)
            {
                if (!CheckAlreadyHaveListener(listenInfo))
                    mListenInfoList.Add(listenInfo);
            }
            else
            {
                mListenInfoList.Add(listenInfo);
            }

            return true;
        }

        /// <summary>
        /// 서버모듈에서 사용되는 정보를 세팅한다(*서버모듈과 ServerBase는 1:1관계)
        /// </summary>
        /// <typeparam name="TServerModule"> 생성할 서버모듈 타입</typeparam>
        /// <typeparam name="TServerInfo">   서버모듈에서 사용. 초기화에 사용될 Config 파일 타입</typeparam>
        /// <typeparam name="TNetworkSystem">서버모듈에서 사용. 초기화에 사용될 통신(accept/connect)객체 </typeparam>
        /// <param name="creater"></param>
        /// <returns>true/false(mServerModuleList의 원소는 최소 1개 이상 존재해야한다)</returns>
        /// <exception cref="ArgumentNullException"></exception>
        private bool SetupSocketServer<TServerModule, TServerInfo, TNetworkSystem>(TServerInfo config, Func<Session> creater)
            where TServerModule : ServerModuleBase, new()
            where TServerInfo : ServerConfig, new()
            where TNetworkSystem : NetworkSystemBase, new()
        {
            try
            {
                mServerModuleList = mServerModuleList ?? new List<IServerModule>();

                for(int idx = 0; idx < mListenInfoList.Count; ++idx)
                {
                    var newModule = new TServerModule();
                    newModule.Initialize(mListenInfoList, mListenInfoList[idx], config, new TNetworkSystem(), Logger, creater);
                    mServerModuleList.Add(newModule);
                }

                return mServerModuleList.Count > 0;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 세션 매니저 세팅. 세션 매니저는 필수 옵션이 아니기 때문에 세팅관련 메서드를 따로 빼두었다
        /// </summary>
        /// <typeparam name="TSessionManager"></typeparam>
        public void SetupSessionManager<TSessionManager>() where TSessionManager : ISessionManager, new()
        {
            foreach(var module in mServerModuleList)
            {
                module.InitializeSessionManager(new TSessionManager());
            }
        }

        /// <summary>
        /// 패킷 송수신(accept, connect) 관련 Thread 세팅
        /// 각 서버모듈 하나당 스레드가 하나씩 할당된다
        /// </summary>
        protected bool SetupThreads()
        {
            foreach (var module in mServerModuleList)
            {
                if (ThreadManager.GetAvailableCount > 0)
                {
                    ThreadManager.AddThread(module.Name, () => ModuleThreadStart(module), true);
                }
                else
                {
                    // 필요한 ip, port 대역대의 서버가 모두 세팅이 되지 않는한 모두 시작하지 않는다
                    Logger.Error("Error in ServerBase.SetupThreads() - ThreadManager' number of ThreadCount is max!!!");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// ServerModule 구동시 진행되는 작업. 등록한 리스너마다 스레드가 1씩 할당되어 accept, connect 및 패킷 송수신 진행
        /// </summary>
        /// <param name="module"></param>
        private void ModuleThreadStart(IServerModule module)
        {
            // ServerModule 상태변경 
            var oldState = ServerState.Initialized;
            if (!ChangeState(oldState, ServerState.Running))
            {
                Logger.Error($"Error in ServerBase.ModuleThreadStart() - State is [{oldState}]. It can be [Running] when state is [Initialized]");
                return;
            }

            if (!module.StartOnce())
            {
                Logger.Error("Error in ServerBase.ModuleThreadStart() - Fail to set StartOnce work!!!");
                return;
            }

            // ServerModule 구동 
            if (!module.Start())
            {
                // 구동 중인 ServerModule에 문제가 생겼을 때 진입. Stop 및 후처리 진행
                // NetworkSystem.Stop -> ServerModule.Stop -> Server.Stop
                mServerState = ServerState.NotStarted;
                module.Stop();

                var ipEndPoint = module.ipEndPoint;
                Logger.Error($"Error in ServerBase.ModuleThreadStart() - Server[{ipEndPoint.Address}:{ipEndPoint.Port}] can't be started!!!");               
            }
        }

        public virtual void Stop()
        {

        }
    }
}
