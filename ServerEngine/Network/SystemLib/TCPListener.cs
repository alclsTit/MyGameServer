using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using ServerEngine.Common;
using ServerEngine.Network.Server;
using ServerEngine.Network.ServerSession;
using ServerEngine.Config;
using ServerEngine.Log;

namespace ServerEngine.Network.SystemLib
{
    /// <summary>
    /// TCP Socket을 이용한 Listen 및 Accept 관련 작업 클래스
    /// </summary>
    public class TCPListener : NetworkSystemBase
    {
        /// <summary>
        /// Client와의 Listen에 사용되는 소켓
        /// </summary>
        public Socket mListenSocket { get; private set; }

        /// <summary>
        /// Socket 통신용 비동기 객체 선언 
        /// </summary>
        private SocketAsyncEventArgs mAcceptEvent;

        /// <summary>
        /// TCPListener 멤버 데이터 초기화 
        /// * 객체 인스턴스 생성 후 가장 먼저 호출하여 멤버 초기화 진행
        /// </summary>
        /// <param name="listenInfo"></param>
        /// <param name="serverInfo"></param>
        /// <param name="logger"></param>
        /// <param name="creater"></param>
        public override void Initialize(ServerModuleBase module, IListenInfo listenInfo, ServerConfig serverInfo, Logger logger, Func<Session> creater)
        {
            try
            {
                base.Initialize(module, listenInfo, serverInfo, logger, creater);

                var oldState = ServerState.NotInitialized;
                if (!ChangeState(oldState, ServerState.Initialized))
                {
                    logger.Error(this.ClassName(), this.MethodName(), $"TCPListener state is [{oldState}]. TCPListener can be Initialized when state is [NotInitialized]");
                    return;
                }
            }
            catch (ArgumentNullException argNullEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), argNullEx);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
            }
        }

        /// <summary>
        /// EndPoint에 binding된 Socket으로 Listen 및 Accept 진행 (Listen -> Accept 순서대로 로직 진행)
        /// </summary>
        /// <returns></returns>
        public override bool Start()
        {
            var oldState = NetworkSystemState.Initialized;
            if (!ChangeState(oldState, NetworkSystemState.Running))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"TCPListener state is [{oldState}]. TCPListener can be started when state is [Initialized]");
                return false;
            }
           
            if (logger.IsDebugEnabled)
                logger.Debug("TCPListener [Accept] Started");

            StartAccept(mAcceptEvent);

            return true;
        }

        /// <summary>
        /// 한 번만 진행되어야하는 Start 작업
        /// </summary>
        /// <returns></returns>
        public override bool StartOnce()
        {
            if (!StartListen())
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Fail to start [StartListen]");
                return false;
            }
            else
            {
                if (logger.IsDebugEnabled)
                    logger.Debug($"TCPListener [Listen({GetIPEndPoint.Address}:{GetIPEndPoint.Port})] Connected");
            }

            return true;
        }

        /// <summary>
        /// Listen 진행. 한번 Binding된 ip, port는 중복으로 binding 될 수 없다
        /// </summary>
        /// <returns></returns>
        private bool StartListen()
        {
            var ipEndPoint = GetIPEndPoint;
            if (ipEndPoint == null)
            {
                logger.Error(this.ClassName(), this.MethodName(), "IPEndPoint is null");
                return false;
            }

            mListenSocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Listen socket 옵션 세팅
            mListenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            mListenSocket.NoDelay = mServerInfo.nodelay;
            mListenSocket.LingerState = new LingerOption(false, 0);

            // Listen bind 및 listen 진행
            mListenSocket.Bind(ipEndPoint);
            mListenSocket.Listen(mListenInfo.backlog);

            mAcceptEvent = new SocketAsyncEventArgs();
            mAcceptEvent.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

            return true;
        }

        private void StartAccept(SocketAsyncEventArgs e)
        {
            var lPending = mListenSocket.AcceptAsync(e);
            if (!lPending)
                OnAcceptCompleted(null, e);
        }

        private void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (!AsyncCallbackChecker.CheckCallbackHandler_SocketError(e.SocketError))
            {
                logger.Error(this.ClassName(), this.MethodName(), $"[SocketError = {e.SocketError}");
                return;
            }

            try
            {
                if (logger.IsDebugEnabled)
                    logger.Debug("Accept Completed!!!");

                Session session = null;
                var serverName = mServerModule.GetConnectedServerName(e.AcceptSocket.LocalEndPoint);
                if (serverName == default(string))
                {
                     // 클라이언트 접속
                    if (!mServerModule.mSessionManager.CheckConnectionMax())
                    {
                        session = mServerModule.NewClientSessionCreate(Guid.NewGuid().ToString(), e, logger, mSessionCreater, true);
                        if (session != null)
                            OnSessionCreateCompleted(session, e);
                    }
                    else
                    {
                        logger.Error(this.ClassName(), this.MethodName(), $"Session[{GetIPEndPoint.Address}:{GetIPEndPoint.Port}] is Full!!!");
                    }
                }
                else
                {
                    if (!mServerModule.mSessionManager.CheckMultiConnected(e.AcceptSocket.LocalEndPoint))
                    {
                        // 서버 접속 
                        session = mServerModule.NewClientSessionCreate(serverName, e, logger, mSessionCreater, false);
                        if (session != null)
                            OnSessionCreateCompleted(session, e);
                    }
                    else
                    {
                        // 이미 해당 IP, PORT로 소켓 연결된 서버가 존재한다. 해당 연결 Close 처리진행
                        Stop();
                        return; 
                    }
                }
      
                // Socket 비동기 객체를 재사용하기 위해서 AcceptSocket null처리 및 StartAccept 재호출
                e.AcceptSocket = null;
                StartAccept(e);
            }
            catch (Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
                return; 
            }
        }

        /// <summary>
        /// 세션 생성 후 작업
        /// 1. 세션 매니저에 세션 추가 
        /// 2. 세션 Recv 시작
        /// 3. 세션 Connected 이후 콜백메서드 호출에 따른 작업 진행
        /// </summary>
        /// <param name="session"></param>
        /// <param name="e"></param>
        private void OnSessionCreateCompleted(Session session, SocketAsyncEventArgs e)
        {
            try
            {
                mServerModule.mSessionManager.AddSession(session);
                session.StartReceive();
                session.OnConnected(e.AcceptSocket.LocalEndPoint, session);
            }
            catch (ArgumentNullException argNullEx)
            {
                logger.Error(this.ClassName(), this.MethodName(), argNullEx);
                return;
            }
            catch(Exception ex)
            {
                logger.Error(this.ClassName(), this.MethodName(), ex);
                return;
            }
        }

        /// <summary>
        /// TCPListener 중지 
        /// 임의의 Task 스레드에서 호출가능하기 때문에 ThreadSafe 하게 작업
        /// </summary>
        public override void Stop()
        {
            if (!CheckCanStop())
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Fail to stop - TCPListener is already [{mSystemState}]");
                return;
            }
            else
            {
                if (!ChangeState(mSystemState, NetworkSystemState.Stopping))
                {
                    logger.Error($"TCPListener state is [{mSystemState}]. Fail to change state [Stopping]");
                    return;
                }
            }

            if (mListenSocket == null)
            {
                logger.Error(this.ClassName(), this.MethodName(), $"Fail to stop - ListenSocket is null");
                return;
            }

            lock (mLockObject)
            {
                try
                {
                    if (mListenSocket.Connected)
                    {
                        mListenSocket.Shutdown(SocketShutdown.Both);
                        mListenSocket.Close();
                    }

                    mAcceptEvent.Completed -= new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);
                    mAcceptEvent.Dispose();
                }
                catch (Exception ex)
                {
                    logger.Error(this.ClassName(), this.MethodName(), ex);
                    return;
                }
                finally
                {
                    mAcceptEvent = null;
                    mListenSocket = null;
                    mSystemState = NetworkSystemState.StopCompleted;
                }

                OnStopCallback();
            }
        }    
    }
}
