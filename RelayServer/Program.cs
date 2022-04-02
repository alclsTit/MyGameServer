using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RelayServer.Config;

using ServerEngine.Network.Server;
using ServerEngine.Network.SystemLib;

namespace RelayServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var csServer = new ControlServer();
            var config = new RelayServerConfig();
            var listenInfoList = new List<IListenInfo>();
            try
            {
                if (!csServer.Initialize("CSServer", "CSServer"))
                {
                    Console.WriteLine("Fail to set Logger. Logger must be set before starting server");
                    return;
                }

                ConfigLoader.Instance.Initialize("ConnectInfo");

                if (ConfigLoader.Instance.LoadConfig(out config))
                {
                    if (ConfigLoader.Instance.LoadListeners(listenInfoList))
                    {
                        if (ConfigLoader.Instance.LoadMssqlConfig("DBServer") /*&& ConfigLoader.Instance.LoadMysqlConfig("DBServer")*/)
                        {
                            if (!csServer.Setup<ServerAcceptModule, RelayServerConfig, TCPListener>(listenInfoList, config, () => new RelaySession()))
                            {
                                csServer.logger.Error($"Exception in Program.Main - Fail to [setup Server]");
                                return;
                            }
                            else
                            {
                                csServer.SetupSessionManager<ServerSessionManager>();
                                csServer.Start();
                            }
                        }
                        else
                        {
                            csServer.logger.Error($"Exception in Program.Main - Fail to [Load DBConfig]");
                            return;
                        }
                    }
                    else
                    {
                        csServer.logger.Error($"Exception in Program.Main - Fail to [Load ServerListeners]");
                        return;
                    }
                }
                else
                {
                    csServer.logger.Error($"Exception in Program.Main - Fail to [Load Config]");
                    return;
                }

                while (true) { }
            }
            catch (Exception ex)
            {
                csServer.logger.Error($"Exception in Program.Main - {ex.Message} - {ex.StackTrace}");
            }
        }
    }
}
