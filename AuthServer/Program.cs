using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AuthServer.Config;
using AuthServer.Handler;

using ServerEngine.Log;
using ServerEngine.Network.Server;
using ServerEngine.Network.SystemLib;

namespace AuthServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var authServer = new ControlServer();
            var config = new AuthServerConfig();
            var listenInfoList = new List<IListenInfo>();
            try
            {
                if (!authServer.Initialize("AuthServer", "AuthServer"))
                {
                    Console.WriteLine("Fail to set Logger. Logger must be set before starting server");
                    return;
                }

                MessageReceiver.Initialize();
                ConfigLoader.Instance.Initialize("AuthConnectInfo");
                if (ConfigLoader.Instance.LoadConfig(out config))
                {
                    if (ConfigLoader.Instance.LoadListeners(listenInfoList))
                    {
                        if (ConfigLoader.Instance.LoadMssqlConfig("127.0.0.1") /*&& ConfigLoader.Instance.LoadMysqlConfig("DBServer")*/)
                        {
                            if (!authServer.Setup<ServerConnectModule, AuthServerConfig, TCPConnecter>(listenInfoList, config, () => new AuthSession()))
                            {
                                authServer.logger.Error($"Exception in Program.Main - Fail to [setup Server]");
                                return;
                            }
                            else
                            {
                                authServer.SetupSessionManager<ServerSessionManager>();
                                authServer.Start();
                            }
                        }
                        else
                        {
                            authServer.logger.Error($"Exception in Program.Main - Fail to [Load DBConfig]");
                            return;
                        }
                    }
                    else
                    {
                        authServer.logger.Error($"Exception in Program.Main - Fail to [Load ServerListeners]");
                        return;
                    }
                }
                else
                {
                    authServer.logger.Error($"Exception in Program.Main - Fail to [Load Config]");
                    return;
                }

                while (true) { }
            }
            catch (Exception ex)
            {
                authServer.logger.Error($"Exception in AuthServer.Program.Main - {ex.Message} - {ex.StackTrace}");
            }
        }
    }
}
