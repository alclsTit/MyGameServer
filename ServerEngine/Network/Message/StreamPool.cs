using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using ServerEngine.Common;

namespace ServerEngine.Network.Message
{
    public interface IResetStreamObject
    {
        bool Reset();
    }

    public class RecvStream
    {

    }

    public class SendStream
    {

    }

    /// <summary>
    /// StreamPool의 크기 조정은 외부에서 가능하나, 실시간 리로드가 안되도록 구조설계. 위험성존재
    /// </summary>
    /// <typeparam name="TStream"></typeparam>
    public abstract class StreamPool<TStream> where TStream : class, IResetStreamObject, new()
    {
        private DefaultObjectPoolProvider mDefaultObjectPoolProvider = new DefaultObjectPoolProvider();

        #region property
        public int MaxTaskCount { get; private set; }
        public int Capacity { get; private set; }
        public int CapacityPerThread { get; private set; }
        public ConcurrentDictionary<int, NonDisposableObjectPool<TStream>> mObjectPoolList { get; private set; } = new ConcurrentDictionary<int, NonDisposableObjectPool<TStream>>();
        #endregion

        protected StreamPool(int max_task_count, int default_size, IPooledObjectPolicy<TStream>? policy = default)
        {
            MaxTaskCount = max_task_count;
            Capacity = default_size;
            CapacityPerThread = (int)(default_size / max_task_count);

            if (null != policy)
            {
                for (int i = 1; i <= max_task_count; ++i)
                    mObjectPoolList.TryAdd(i, new NonDisposableObjectPool<TStream>(policy, default_size));
            }
            else
            {
                for (int i = 1; i <= max_task_count; ++i)
                    mObjectPoolList.TryAdd(i, new NonDisposableObjectPool<TStream>(new DefaultPooledObjectPolicy<TStream>(), default_size));
            }
        }

        public virtual TStream Get(int worker_index)
        {
            return mObjectPoolList[worker_index].Get();
        }

        public virtual void Return(int worker_index, TStream obj)
        {
            obj.Reset();
            mObjectPoolList[worker_index].Return(obj);
        }
    }

    public sealed class RecvStreamPool : StreamPool<RecvStream>
    {
        public RecvStreamPool() : base()
        {

        }


        public override void Return(int worker_index, RecvStream obj)
        {
            base.Return(worker_index, obj);
        }
    }
}
