using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Common
{
    public enum eServerType
    {
        None = 0,
        Game = 1,
        Indun = 2,
        GameManager = 3,
        IndunManager = 4,
        Gate = 5
    }

    public interface IAsyncEventCallbackHandler
    {
        public delegate void AsyncEventCallbackHandler(object? sender, SocketAsyncEventArgs e);
    }

    public static class Utility
    {
        public static string? GetServerNameByType(eServerType type)
        {
            switch(type)
            {
                case eServerType.Game:
                    return "GameServer";
                case eServerType.Indun:
                    return "IndunServer";
                case eServerType.GameManager:
                    return "GameManagerServer";
                case eServerType.IndunManager:
                    return "IndunManagerServer";
                case eServerType.Gate:
                    return "GateServer";
            }

            return string.Empty;
        }
    }
}
