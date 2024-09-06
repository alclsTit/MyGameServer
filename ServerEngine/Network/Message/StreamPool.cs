﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using ServerEngine.Common;

namespace ServerEngine.Network.Message
{
    #region Stream
    public interface IStream
    {
        public int Tag { get; }
        ArraySegment<byte> Buffer { get; }
        
        void SetTag(int index);
        bool Reset();
    }

    public abstract class Stream : IStream
    {
        #region property
        public int Tag { get; private set; }
        public ArraySegment<byte> Buffer { get; protected set; }
        #endregion

        protected Stream(int buffer_size) 
        { 
            Buffer = new ArraySegment<byte>(new byte[buffer_size]); 
        }

        public virtual bool Initialize()
        {
            // Todo: 추후 생성자외의 메서드에서 초기화가 필요할 경우 작업
            return true;
        }

        public void SetTag(int index)
        {
            Tag = index;
        }

        public virtual bool Reset()
        {
            Tag = 0;
            if (null != Buffer.Array)
                Array.Clear(Buffer.Array, 0, Buffer.Array.Length);

            return true;
        }
    }

    public class RecvStream : Stream
    {

        public RecvStream(int buffer_size) 
            : base(buffer_size) 
        { 
        }
       
        public override bool Reset()
        {
            if (!base.Reset())
                return false;

            return true;
        }
    }

    public class SendStream : Stream
    {
        public SendStream(int buffer_size) 
            : base(buffer_size) 
        { 
        }

        public override bool Reset()
        {
            if (!base.Reset()) 
                return false;

            return true;
        }
    }
    #endregion

    #region StreamPool
    /// <summary>
    /// StreamPool의 크기 조정은 외부에서 가능하나, 실시간 리로드가 안되도록 구조설계. 위험성존재
    /// </summary>
    /// <typeparam name="TStream"></typeparam>
    public abstract class StreamPool<TStream> where TStream : class, IStream
    {
        private static volatile int mShareCount;
        #region property
        public int MaxWorkerCount { get; private set; }
        public int Capacity { get; private set; }
        public int CapacityPerThread { get; private set; }
        public ConcurrentDictionary<int, NonDisposableObjectPool<TStream>> mObjectPoolList { get; private set; } = new ConcurrentDictionary<int, NonDisposableObjectPool<TStream>>();
        #endregion

        protected StreamPool(int max_worker_count, int default_size, IPooledObjectPolicy<TStream> policy)
        {
            MaxWorkerCount = max_worker_count;
            Capacity = default_size;
            CapacityPerThread = (int)(default_size / max_worker_count);

            for (int i = 1; i <= max_worker_count; ++i)
                mObjectPoolList.TryAdd(i, new NonDisposableObjectPool<TStream>(policy, default_size));
        }

        private bool CheckIndexRange(int index)
        {
            return 0 < index && index <= MaxWorkerCount;
        }

        #region public_method
        public virtual TStream? Get(int index)
        {
            if (!CheckIndexRange(index))
                return default;

            return mObjectPoolList[index].Get();
        }

        public virtual TStream? Get()
        {
            int increased = Interlocked.Increment(ref mShareCount);
            int index = increased % MaxWorkerCount;
            if (!CheckIndexRange(index))
                return default;

            var stream = mObjectPoolList[index].Get();
            stream.SetTag(index);

            return stream;
        }

        public virtual void Return(TStream obj)
        {
            var index = obj.Tag;
            if (!CheckIndexRange(index))
                return;

            obj.Reset();
            mObjectPoolList[index].Return(obj);
        }

        public virtual void Return(int index, TStream obj)
        {
            if (!CheckIndexRange(index))
                return;

            obj.Reset();
            mObjectPoolList[index].Return(obj);
        }
        #endregion
    }

    public sealed class RecvStreamPool : StreamPool<RecvStream>
    {
        public RecvStreamPool(int max_worker_count, int default_size, int recv_buffer_size) 
            : base(max_worker_count, default_size, new RecvStreamObjectPoolPolicy(recv_buffer_size))
        {
        }
    }

    public sealed class SendStreamPool : StreamPool<SendStream>
    {
        public SendStreamPool(int max_worker_count, int default_size, int send_buffer_size) 
            : base(max_worker_count, default_size, new SendStreamObjectPoolPolicy(send_buffer_size))
        {
        }
    }
    #endregion
}
