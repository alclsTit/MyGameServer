using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using ServerEngine.Common;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// SocketAsyncEventArgs에 대한 ObjectPooling
    /// </summary>
    /// 사용하지 않음. Microsoft.Extensions.ObjectPool로 대체 
    /*public class SocketAsyncEventArgsPool
    {
        /// <summary>
        /// SocketAsyncEventArgs 메모리 풀 객체
        /// </summary>
        private ObjectPool<SocketAsyncEventArgs> mAsyncObjectPool;

        /// <summary>
        /// 오브젝트풀에서 관리하는 총 SocketAsyncEventArgs 객체 수
        /// </summary>
        public int TotalCount { get; private set; }

        /// <summary>
        /// 오브젝트풀에서 관리되는 SocketAsyncEventArgs 객체 수
        /// </summary>
        public int Count => mAsyncObjectPool.Count;

        /// <summary>
        /// 풀링에 존재하는 객체가 비었는지 
        /// </summary>
        public bool IsEmpty => mAsyncObjectPool.IsEmpty;

        public SocketAsyncEventArgsPool(int maxConnector)
        {
            TotalCount = maxConnector;
            mAsyncObjectPool = new ObjectPool<SocketAsyncEventArgs>(maxConnector, () => new SocketAsyncEventArgs());
        }

        ~SocketAsyncEventArgsPool()
        {
            mAsyncObjectPool.Dispose();
        }    

        /// <summary>
        /// ThreadSafe Queue Push
        /// </summary>
        /// <param name="item"></param>
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null)
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");

            mAsyncObjectPool.Push(item);
        }

        /// <summary>
        /// ThreadSafe Queue Pop
        /// </summary>
        /// <returns> 
        /// 1. Pooling에 존재하던 SocketAsyncEventArgs
        /// 2. 기본값으로 초기화된 SocketAsyncEventArgs
        /// </returns>
        public SocketAsyncEventArgs Pop()
        {
            return mAsyncObjectPool.Pop();
        }

        //Todo: Reset 기능을 추가할지 말지... 추가한다면 수정필요
        public void Reset(int resizedMaxConnector, Func<SocketAsyncEventArgs> createFunc)
        {
            mAsyncObjectPool.Reset(resizedMaxConnector, createFunc);

        }
    }
    */
}

