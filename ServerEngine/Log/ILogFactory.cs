using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Log
{
    /// <summary>
    /// Factory Method Design Pattern
    /// </summary>
    public interface ILogFactory
    {
        Logger GetLogger(string name, string nameOfConsoleTitle);
    }
}
