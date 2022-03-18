using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// ThreadPool Manager
    /// default Workthread(8 - my cpucore ~ 2047) - default IOThread(8 - my cpucore ~ 1000) 
    /// ThreadPool은 간단한 작업, 무겁지 않은 작업을 대상으로 사용 및 프로그램 실행 중 계속 실행되어야하는 작업은 ThreadPool이 아닌 Thread를 사용한다
    /// </summary>
    public static class ThreadPoolEx
    {
        public static int GetAvailableWorkThreadNum()
        {
            ThreadPool.GetAvailableThreads(out int workThreads, out int IOThreads);
            return workThreads;
        }

        public static int GetAvailableIOThreadNum()
        {
            ThreadPool.GetAvailableThreads(out int workThreads, out int IOThreads);
            return IOThreads;
        }

        /// <summary>
        /// ThreadPool에서 관리하는 스레드 갯수 최소, 최대 값 설정
        /// 스레드 갯수를 0으로 지정 시, 디폴트 값으로 저장 (min = 자신의 cpu core갯수 / max = 2047(worker) - 1000(IO))
        /// </summary>
        /// <param name="numOfWorkThreadMin"></param>
        /// <param name="numOfWorkThreadMax"></param>
        /// <param name="numOfIOThreadMin"></param>
        /// <param name="numOfIOThreadMax"></param>
        /// <returns></returns>
        public static bool ResetThreadPoolInfo(int numOfWorkThreadMin, int numOfWorkThreadMax, int numOfIOThreadMin, int numOfIOThreadMax)
        {
            if (numOfWorkThreadMin > 0 || numOfIOThreadMin > 0)
            {
                ThreadPool.GetMinThreads(out int oldWorkThreadMin, out int oldIOThreadMin);

                if (numOfWorkThreadMin <= 0)
                    numOfWorkThreadMin = oldWorkThreadMin;

                if (numOfIOThreadMin <= 0)
                    numOfIOThreadMin = oldIOThreadMin;

                if (numOfWorkThreadMin != oldWorkThreadMin || numOfIOThreadMin != oldIOThreadMin)
                {
                    if (!ThreadPool.SetMinThreads(numOfWorkThreadMin, numOfIOThreadMin))
                        return false;
                }
            }

            if (numOfWorkThreadMax > 0 || numOfIOThreadMax > 0)
            {
                ThreadPool.GetMaxThreads(out int oldWorkThreadMax, out int oldIOThreadMax);

                if (numOfWorkThreadMax <= 0 || numOfWorkThreadMax < numOfWorkThreadMin)
                    numOfWorkThreadMax = oldWorkThreadMax;

                if (numOfIOThreadMax <= 0 || numOfIOThreadMax < numOfIOThreadMin)
                    numOfIOThreadMax = oldIOThreadMax;

                if (numOfWorkThreadMax != oldWorkThreadMax || numOfIOThreadMax != oldIOThreadMax)
                {
                    if (!ThreadPool.SetMaxThreads(numOfWorkThreadMax, numOfIOThreadMax))
                        return false;
                }
            }

            return true;
        }
    }
}
