using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Common
{
    /// <summary>
    /// 패킷 풀링, 메모리풀 등 여러 곳에서 사용할 수 있으므로 싱글턴 적용 x
    /// [조건]
    /// 1. Thread-safe
    /// 2. 다양한 객체에 대응 
    /// 3. 메모리 파편화가 발새하지 않아야된다 
    /// 4. lock-free
    /// </summary>
    /// <typeparam name="T">풀링할 객체인 T는 참조타입이면서 디폴트 생성자가 존재해야한다</typeparam>
    public class ObjectPool<T> where T : IDisposable, new()
    {
        private bool mAlreadyDisposed = false;
        private Queue<T> mQueue = new Queue<T>();
        private Func<T> mCreateFunc;
        public int mMaxPoolingCount;
        // Thread-Safe
        public int Count => mQueue.Count;
        // Thread-Safe
        public bool IsQueueCountMax => this.Count == mMaxPoolingCount;
        // Thread-Safe
        public bool IsEmpty => this.Count <= 0;

        private object mCriticalObject = new object();

        public ObjectPool(int poolSize, Func<T> createFunc)
        {
            this.Init(poolSize, createFunc);
        }

        private void Init(int poolSize, Func<T> createFunc)
        {
            mMaxPoolingCount = poolSize;

            if (createFunc == null)
                mCreateFunc = () => new T();
            else
                mCreateFunc = createFunc;

            for (int i = 0; i < poolSize; ++i)
                mQueue.Enqueue(mCreateFunc());
        }

        public void Reset(int poolSize, Func<T> createFunc)
        {
            this.Clear();

            lock (mCriticalObject)
            {
                Init(poolSize, createFunc);
            }
        }

        public void Push(T data)
        {
            if (this.TryPush(data))
            {
                lock (mCriticalObject)
                {
                    mQueue.Enqueue(data);
                }
            }
        }

        private bool TryPush(T data)
        {
            if (data == null)
                return false;

            if (this.IsQueueCountMax)
                return false;

            return true;
        }

        public T Pop()
        {
            if (this.IsEmpty)
                return new T();

            T result;
            lock (mCriticalObject)
            {
                result = mQueue.Peek();
                mQueue.Dequeue();
            }

            return result;
        }

        public void Clear()
        {
            lock (mCriticalObject)
            {
                mQueue.Clear();
            }
        }

        public IEnumerator<T> GetPoolSequence()
        {
            var Enumerator = mQueue.GetEnumerator();
            while (Enumerator.MoveNext())
            {
                yield return Enumerator.Current;
            }
        }

        protected virtual void Dispose(bool disposed)
        {
            if (!mAlreadyDisposed)
            {
                if (disposed)
                {
                    for (int i = 0; i < this.Count; ++i)
                    {
                        var target = mQueue.Peek();
                        target.Dispose();
                        mQueue.Dequeue();
                    }
                }

                mAlreadyDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
