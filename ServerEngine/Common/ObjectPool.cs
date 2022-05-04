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
    /// 3. 메모리 파편화가 발생하지 않아야된다 
    /// </summary>
    /// <typeparam name="T">풀링할 객체인 T는 참조타입이면서 디폴트 생성자가 존재해야한다</typeparam>
    public class ObjectPool<T> where T : class, IDisposable, new()
    {
        public int mMaxPoolingCount;
        private bool mAlreadyDisposed = false;
        private Queue<T> mQueue;
        private Func<T> mCreateFunc;
        private object mCriticalObject = new object();

        #region "Property"
        // Thread-Safe
        public int Count => mQueue.Count;
        // Thread-Safe
        public bool IsQueueCountMax => this.Count == mMaxPoolingCount;
        // Thread-Safe
        public bool IsEmpty => this.Count <= 0;
        #endregion

        public ObjectPool(int poolSize, Func<T> createFunc)
        {
            mMaxPoolingCount = poolSize;
            mQueue = new Queue<T>(poolSize);

            Initialize(createFunc);
        }

        ~ObjectPool()
        {
            Dispose(false);
        }

        private void Initialize(Func<T> createFunc)
        {
            if (createFunc == null)
                mCreateFunc = () => { return new T(); };
            else
                mCreateFunc = createFunc;

            for (int i = 0; i < mMaxPoolingCount; ++i)
                mQueue.Enqueue(mCreateFunc());
        }

        public void Reset(int poolSize, Func<T> createFunc)
        {
            this.Clear();

            lock (mCriticalObject)
            {
                mMaxPoolingCount = poolSize;
                mQueue = new Queue<T>(poolSize);

                Initialize(createFunc);
            }
        }

        public void Push(T data)
        {
            lock (mCriticalObject)
            {
                if (!TryPush(data))
                    data?.Dispose();

                mQueue.Enqueue(data);
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
            lock(mCriticalObject)
            {
                if (!IsEmpty)
                    return mQueue.Dequeue();

                return mCreateFunc();
            }
        }

        public void Clear()
        {
            lock (mCriticalObject)
            {
                mQueue.Clear();
                mQueue = null;
            }
        }

        protected virtual void Dispose(bool disposed)
        {
            lock(mCriticalObject)
            {
                if (!mAlreadyDisposed)
                {
                    if (disposed)
                    {
                        while(mQueue.Count > 0)
                        {
                            var target = mQueue.Dequeue();
                            target.Dispose();
                        }
                    }

                    mAlreadyDisposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
