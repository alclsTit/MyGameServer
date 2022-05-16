using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ServerEngine.Common
{
    public static class ThreadManager
    {
        // 현재 커스텀 스래드 갯수
        private static int msCustomThreadCount = 0;

        // 현재 추가 가능한 커스텀 스레드 총 갯수 ThreadSafe
        public static int GetAvailableCount => GetCustomThreadMaxCount - GetCustomThreadCount;

        // 현재 구동중인 커스텀 스레드 총 갯수 - ThreadSafe
        public static int GetCustomThreadCount => msCustomThreadCount;

        // 커스텀 스레드 총 갯수 - ThreadSafe
        public static int GetCustomThreadMaxCount => Environment.ProcessorCount;

        /// <summary>
        /// 스레드 관리 컨테이너 (key = 스레드 이름 / value = 스레드객체)
        /// </summary>
        private static Dictionary<string, Thread> mCustomThreads = new Dictionary<string, Thread>();
        
        public static async Task<bool> TryAddThreadTask(string name, Action work, bool isBackground = false)
        {
            return await AddThreadTask(name, work, isBackground);
        }

        /// <summary>
        /// 관리대상이 되는 Thread 추가. 직접 Thread를 생성하여 관리하는 대상은 많지 않을 것이므로 따로 객체풀링하지 않음
        /// </summary>
        /// <param name="work"></param>
        public static Task<bool> AddThreadTask(string name, Action work, bool isBackground)
        {
            var threadObj = new Thread(new ThreadStart(work));
            threadObj.Name = name;
            threadObj.IsBackground = isBackground;

            mCustomThreads.Add(name, threadObj);

            Interlocked.Increment(ref msCustomThreadCount);

            return Task.FromResult<bool>(true);
        }

        public static void AddThread(string name, Action work, bool isBackground)
        {
            var threadObj = new Thread(new ThreadStart(work));
            threadObj.Name = name;
            threadObj.IsBackground = isBackground;

            mCustomThreads.Add(name, threadObj);

            Interlocked.Increment(ref msCustomThreadCount);
        }

        public static bool RemoveThread(string name)
        {
            return mCustomThreads.Remove(name);
        }

        public static bool IsThreadAlive(string name)
        {
            return mCustomThreads[name].IsAlive;
        }

        public static ThreadState GetThreadState(string name)
        {
            return mCustomThreads[name].ThreadState;
        }

        public static void StartThreads()
        {
            foreach (var threadObj in mCustomThreads)
                threadObj.Value.Start();
        }

        public static IEnumerator<KeyValuePair<string, Thread>> GetCustomThreads()
        {
            using (var lEnumerator = mCustomThreads.GetEnumerator())
            {
                while (lEnumerator.MoveNext())
                    yield return lEnumerator.Current;
            }
        }
    }
}
