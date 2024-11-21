using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using ServerEngine.Common;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;
using ServerEngine.Network.Server;
using System.Data;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// TCP Socket을 이용한 Listen 및 Accept 관련 작업 클래스 (ServerModule 당 여러개 할당가능)
    /// </summary>
    public class TcpAcceptor : NetworkSystemBase
    {
        /// <summary>
        /// listen socket that used on acceptor
        /// </summary>
        private TcpSocket? mListenSocket;

        /// <summary>
        /// Listen / Pool Config
        /// </summary>
        private IConfigListen? m_config_listen;
        private IConfigEtc? m_config_etc;

        /// <summary>
        /// Microsoft.Extensions.ObjectPool - AcceptEventArgs
        /// </summary>
        private DisposableObjectPool<SocketAsyncEventArgs>? mAcceptEventArgsPool;

        private Thread mAcceptThread;
        private AutoResetEvent mThreadBlockEvent = new AutoResetEvent(true);
        private int mAcceptCount = 0;

    #region property
        /// <summary>
        /// dependent on server_name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Get ListenSocket
        /// </summary>
        public TcpSocket? GetListenSocket => mListenSocket;
    #endregion

        public TcpAcceptor(string name, Log.ILogger logger, ServerModule module) 
            : base(logger, module)
        {
            Name = name;    
            mAcceptThread = new Thread(() => { StartAccept(); });
        }

        #region private method
        private Socket CreateListenSocket()
        {
            if (null == m_config_listen)
                throw new NullReferenceException(nameof(m_config_listen));

            try
            {
                var socket = new Socket(addressFamily: AddressFamily.InterNetwork, socketType: SocketType.Stream, protocolType: ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Parse(m_config_listen.address), m_config_listen.port));
                socket.Listen(m_config_listen.backlog);

                return socket;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private bool CheckMaxConnection()
        {
            if (null == m_config_listen)
                return false;

            var current_connected_count = Volatile.Read(ref mAcceptCount);
            return current_connected_count < m_config_listen.max_connection ? false : true;
        }
        #endregion

        #region public_method
        public override bool Initialize()
        {
            throw new NotSupportedException();
        }

        public override bool Initialize(IConfigListen config_listen, IConfigEtc config_etc)
        {
            if (null == config_listen) 
                throw new ArgumentNullException(nameof(config_listen));

            if (null == config_etc)
                throw new ArgumentNullException(nameof(config_etc));

            try
            {
                m_config_listen = config_listen;
                m_config_etc = config_etc;

                mListenSocket = new TcpSocket(CreateListenSocket(), base.Logger);

                var pool_default_size = m_config_etc.pools.list.FirstOrDefault(e => e.name.ToLower().Trim() == Name.ToLower().Trim())?.default_size;
                int maximum_retained = true == pool_default_size.HasValue ? pool_default_size.Value : m_config_listen.max_connection;

                mAcceptEventArgsPool = new DisposableObjectPool<SocketAsyncEventArgs>(
                    new SocketEventArgsObjectPoolPolicy(OnAcceptCompleteHandler), 
                    maximum_retained);

                UpdateState(eNetworkSystemState.Initialized);

                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpAcceptor Initialize Complete");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.Initialize() - {ex.Message} - {ex.StackTrace}", ex);
                return false;
            }
        }

        public override bool Start()
        {
            if (false == CheckState(eNetworkSystemState.Initialized) && 
                false == CheckState(eNetworkSystemState.StopComplete))
            {
                Logger.Error($"Error in TcpAcceptor.Start() - TcpAcceptor Can't Start Accept. state = [{(eNetworkSystemState)mState}]");
                return false;
            }

            UpdateState(eNetworkSystemState.Running);

            mAcceptThread.Start();

            if (Logger.IsEnableDebug)
                Logger.Debug($"TcpAcceptor Start Complete");

            return true;
        }

        public override bool Start(string address, ushort port, bool client_connect = true)
        {
            throw new NotSupportedException();
        }

        // 임의의 1개 Thread에서 반복적으로 실행 
        public void StartAccept()
        {
            try
            {
                if (null == mListenSocket)
                    throw new NullReferenceException(nameof(mListenSocket));

                if (null == mAcceptEventArgsPool)
                    throw new NullReferenceException(nameof(mAcceptEventArgsPool));

                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpAcceptor Accpet Start. address = {m_config_listen?.address}, port = {m_config_listen?.port}");

                while (mState == (int)eNetworkSystemState.Running)
                { 
                    while (true == CheckMaxConnection())
                    {
                        // accept된 객체의 수가 현재 max_connection을 초과하게 되는 경우
                        // 기존 connection이 disconnect 될 때까지 추가 connect를 받지 않고 대기
                        Thread.Sleep(100);
                    }

                    mThreadBlockEvent.WaitOne();

                    SocketAsyncEventArgs? accept_event_args = null;
                    try
                    {
                        accept_event_args = mAcceptEventArgsPool.Get();

                        var pending = mListenSocket?.GetSocket?.AcceptAsync(accept_event_args);
                        if (false == pending)
                            OnAcceptCompleteHandler(null, accept_event_args);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Exception in TcpAcceptor.StartAccept() - {ex.Message} - {ex.StackTrace}", ex);
                        if (null != accept_event_args)
                            mAcceptEventArgsPool.Return(accept_event_args);
                        mListenSocket?.DisconnectSocket();
                    }
                }

                Logger.Info($"Info in TcpAcceptor.StartAccept() - TcpAcceptor Accpet Function exit!!!");
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.StartAccept() - {ex.Message} - {ex.StackTrace}", ex);
                mListenSocket?.DisconnectSocket();
                return;
            }
        }

        // accept callback (threadpool thread operated)
        public void OnAcceptCompleteHandler(object? sender, SocketAsyncEventArgs e)
        {
            bool completed = false;
            try
            {       
                var socket_error = e.SocketError;
                if (false == AsyncCallbackChecker.CheckCallbackHandler_SocketError(socket_error))
                {
                    Logger.Error($"Error in TcpAcceptor.OnAcceptCompleteHandler() - SocketError = {socket_error}");
                    return;
                }

                if (e.UserToken is null)
                {
                    Logger.Error($"Error in TcpAcceptor.OnAcceptCompleteHandler() - UserToken is null");
                    return;
                }

                // connection 완료시 UserToken 생성 및 해당 token에 대한 receive / send 로직 처리 진행
                ServerModule.OnNewClientCreateHandler(e, false);        

                mThreadBlockEvent.Set();

                if (Logger.IsEnableDebug)
                    Logger.Debug($"TcpAcceptor Accept Complete");

                completed = true;
            }
            catch (Exception ex)
            {
                completed = false;
                Logger.Error($"Exception in TcpAcceptor.OnAcceptCompleteHandler() - {ex.Message} - {ex.StackTrace}");
            }
            finally
            {
                // 사용된 객체 반환
                mAcceptEventArgsPool?.Return(e);

                if (completed)
                    Interlocked.Increment(ref mAcceptCount);
            }
        }

        // 임의의 Thread에서 실행된 가능성이 있는 로직
        public override void Stop()
        {
            try
            {
                var state = (eNetworkSystemState)mState;
                if (CheckStop())
                {
                    Logger.Error($"Error in TcpAcceptor.Stop() - Stop process is already working. state = [{state}]");
                    return;
                }
              
                if (null == Interlocked.CompareExchange(ref mListenSocket, null, null) ||
                    true == mListenSocket?.IsNullSocket())
                {
                    return;
                }

                UpdateState(eNetworkSystemState.Stopping);

                mThreadBlockEvent.WaitOne();

                if (true == mListenSocket?.GetSocket?.Connected)
                    mListenSocket.Dispose(SocketShutdown.Both);
                
                mAcceptEventArgsPool?.Dispose();

                mThreadBlockEvent.Set();

                UpdateState(eNetworkSystemState.StopComplete);

                if (mAcceptThread.IsAlive)
                    mAcceptThread.Join();
            }
            catch (Exception ex)
            {
                Logger.Error($"Exception in TcpAcceptor.Stop() - {ex.Message} - {ex.StackTrace}", ex);
                return;
            }
        }
        #endregion
    }
}
