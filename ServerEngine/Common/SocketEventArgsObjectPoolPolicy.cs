using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using Microsoft.Extensions.ObjectPool;
using Microsoft.VisualBasic.FileIO;
using ServerEngine.Network.ServerSession;
using ServerEngine.Protocol;
using ServerEngine.Network.Message;

namespace ServerEngine.Common
{
    #region ObjectPoolPolicy - StreamPool (send/recv)
    public class SendStreamObjectPoolPolicy : IPooledObjectPolicy<SendStream>
    {
        private int m_buffer_size;

        public SendStreamObjectPoolPolicy(int buffer_size)
        {
            m_buffer_size = buffer_size;
        }

        public SendStream Create()
        {
            return new SendStream(m_buffer_size);
        }

        public bool Return(SendStream stream)
        {
            if (null == stream)
                return false;

            if (null != stream.Buffer.Array)
                Array.Clear(stream.Buffer.Array, 0, stream.Buffer.Array.Length);

            return true;
        }
    }

    public class RecvStreamObjectPoolPolicy : IPooledObjectPolicy<RecvStream>
    {
        private int m_buffer_size;

        public RecvStreamObjectPoolPolicy(int buffer_size)
        {
            m_buffer_size = buffer_size;
        }

        public RecvStream Create()
        {
            return new RecvStream(m_buffer_size);
        }

        public bool Return(RecvStream stream)
        {
            if (null == stream)
                return false;

            if (null != stream.Buffer.Array)
                Array.Clear(stream.Buffer.Array, 0, stream.Buffer.Array.Length);

            return true;
        }
    }
    #endregion

    #region ObjectPoolPolicy - SocketAsyncEventArgs
    /// <summary>
    /// MS ObjectPool 정책. 풀링하여 사용하는 대상들 개별로 각 정책을 세워서 추가해줘야한다
    /// ObjectPool Get 진행시, empty pool 상태면 policy에 따라서 추가 Get 요청시 create 함수가 실행되어 객체를 반환한다
    /// </summary>
    public class SocketEventArgsObjectPoolPolicy : IPooledObjectPolicy<SocketAsyncEventArgs>, IAsyncEventCallbackHandler
    {
        public delegate bool ResetCallbackHandler(SocketAsyncEventArgs e);

        private IAsyncEventCallbackHandler.AsyncEventCallbackHandler mHandler;
        private UserToken? mUserToken;
        private ResetCallbackHandler? mResetHandler;

        public SocketEventArgsObjectPoolPolicy(IAsyncEventCallbackHandler.AsyncEventCallbackHandler handler, UserToken? user_token = default, ResetCallbackHandler? reset_handler = default)
        {
            mHandler = handler;
            mUserToken = user_token;

            reset_handler = (default == reset_handler)? DefaultResetCallbackHandler : reset_handler; 
        }

        public SocketAsyncEventArgs Create()
        {
            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(mHandler);
            eventArgs.UserToken = mUserToken;

            return eventArgs;
        }

        public bool Return(SocketAsyncEventArgs obj)
        {
            if (null == obj || null == mResetHandler)
                return false;

            return mResetHandler(obj);
        }

        private bool DefaultResetCallbackHandler(SocketAsyncEventArgs e)
        {
            e.AcceptSocket = null;
            e.Completed -= new EventHandler<SocketAsyncEventArgs>(mHandler);
            e.UserToken = null;
            e.BufferList = null;

            if (null != e.Buffer)
            {
                Array.Clear(e.Buffer, 0, e.Buffer.Length);
                e.SetBuffer(0, e.Buffer.Length);
            }

            return true;
        }
    }
    #endregion
}
