using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Net;

// 2022.05.14 세션 매니저 수정 작업 - 세션 추가, 삭제를 이곳으로 옮겨서 진행할 수 있는지...
using System.Net.Sockets;
using ServerEngine.Log;
using ServerEngine.Common;

namespace ServerEngine.Network.ServerSession
{
    public interface ISessionManager
    {
        string sessionID { get; }

        int numberOfMaxConnect { get; }

        void Initialize(int maxConnect);

        void AddSession(Session session);

        void RemoveSession(string id);

        void Close(string id);

        bool CheckConnectionMax();

        bool CheckMultiConnected(EndPoint endPoint);

        // 2022.05.14 세션 매니저 수정 작업 - 세션 추가, 삭제를 이곳으로 옮겨서 진행할 수 있는지...
        //public Session NewClientSessionCreate(string sessionID, SocketAsyncEventArgs e, Logger logger, Func<Session> creater, bool isClient);

        //public void OnSessionClosed(Session session, eCloseReason reason);
    }

    /// <summary>
    /// 세션 관리 매니저
    /// 1. 전체 세션(auth, logic...) 관리 
    /// </summary>
    public abstract class SessionManagerBase : ISessionManager
    {
        public string sessionID { get; private set; }

        public int numberOfMaxConnect { get; private set; }

        private ConcurrentDictionary<string, Session> mContainer = new ConcurrentDictionary<string, Session>();

        public int GetSessionCount() => mContainer.Count;

        private readonly object mLockObject = new object();

        protected SessionManagerBase() { }

        public virtual void Initialize(int maxConnect)
        {
            numberOfMaxConnect = maxConnect;
        }

        /// <summary>
        /// 파라미터로 받은 세션아이디에 해당하는 항목이 존재하는지 확인
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsExist(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            if (mContainer.ContainsKey(id))
                return true;

            return false;
        }

        /// <summary>
        /// 세션추가 가능여부 확인
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        private bool TryAddSession(Session session)
        {
            var id = session.mSessionID;

            if (IsExist(id))
                return false;

            if (mContainer.TryAdd(id, session))
                return true;
            else
                return false;
        }
        
        /// <summary>
        /// 세션관리자에서 관리하도록 새로운 세션 추가
        /// </summary>
        /// <param name="session"></param>
        /// <exception cref="Exception"></exception>
        public void AddSession(Session session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            if (!TryAddSession(session))
                throw new Exception($"Fail to add session[{session.mSessionID}]");
        }

        /// <summary>
        /// 세션삭제 가능여부 확인
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool TryRemoveSession(string id)
        {
            if (mContainer.TryRemove(id, out var session))
                return true;
            else
                return false;
        }

        /// <summary>
        /// 세션관리자에서 관리하는 특정 아이디에 해당하는 세션 삭제
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="Exception"></exception>
        public void RemoveSession(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            if (!TryRemoveSession(id))
                throw new Exception($"Fail to remove session[{id}]");
        }

        /// <summary>
        /// 세션매니저에서 관리하는 최대의 세션 Connection 수를 넘어갔는지 확인
        /// * 만약, 최대관리 가능한 세션수를 넘었을 때, 인원제한이 걸렸다는 정보 전달 및 접속불가로 처리
        /// * ThreadSafe
        /// </summary>
        /// <returns></returns>
        public bool CheckConnectionMax()
        {
            var maxConnect = numberOfMaxConnect;
            return maxConnect <= GetSessionCount() ? true : false;
        }

        /// <summary>
        /// 세션관리자에 파라미터로 넘겨받은 세션아이디에 해당하는 대상이 있는지 확인
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Session GetSessionByID(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            if (mContainer.TryGetValue(id, out var session))
                return session;
           
            return null;
        }

        /// <summary>
        /// 세션관리 컨테이너에서 모든 대상 시퀀스로 반환
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Session> GetEnumerator() 
        {
            lock(mLockObject)
            {
                using(var sequence = mContainer.GetEnumerator())
                {
                    while (sequence.MoveNext())
                    {
                        if (sequence.Current.Value is Session session)
                        {
                            yield return session;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 세션관리 컨테이너에서 조건을 만족하는 대상에 대한 시퀀스 반환
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<Session> GetAllSessions(Func<Session, bool> predicate)  
        {
            lock(mLockObject)
            {
                using(var sequence = mContainer.GetEnumerator())
                {
                    while(sequence.MoveNext())
                    {
                        if (sequence.Current.Value is Session session)
                        {
                            if (predicate == null || predicate(session))
                                yield return session;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 현재 연결된 서버 세션과 동일한 세션을 연결하고자 요청하는지 체크
        /// ThreadSafe
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>현재 세션매니저에 동일한 ip, port를 가진 대상이 있는 경우 True 반환, 그렇지 않으면 false 반환</returns>
        public bool CheckMultiConnected(EndPoint endPoint)
        {
            lock(mLockObject)
            {
                var ipEndPoint = (IPEndPoint)endPoint;
                return mContainer.Values.Where((item) => { return item.mLocalIPEndPoint.Address.ToString() == ipEndPoint.Address.ToString() && item.mLocalIPEndPoint.Port == ipEndPoint.Port; }).ToList().Count > 0;
            }
        }

        public void Close(string id)
        {
            if (!IsExist(id))
                return;

            RemoveSession(id);
        }


        // 2022.05.14 세션 매니저 수정 작업 - 세션 추가, 삭제를 이곳으로 옮겨서 진행할 수 있는지...

        /*
        public virtual Session NewClientSessionCreate(string sessionID, SocketAsyncEventArgs e, Logger logger, Func<Session> creater, bool isClient)
        { 
            if (e.LastOperation != System.Net.Sockets.SocketAsyncOperation.Accept && 
                e.LastOperation != System.Net.Sockets.SocketAsyncOperation.Connect)
            {
                //
            }

            var session = creater.Invoke();
            if (e.LastOperation == SocketAsyncOperation.Accept)
                session.Initialize(sessionID, e.AcceptSocket, mServerInfo, logger, mListenInfoList, isClient);
            else
                session.Initialize(sessionID, e.ConnectSocket, mServerInfo, logger, mListenInfoList, isClient);


            session.SetRecvEventByPool(mRecvEventPoolFix.Get());
            session.SetSendEventByPool(mSendEventPoolFix.Get());

            return session;
        }

        public virtual void OnSessionClosed(Session session, eCloseReason reason)
        {
            if (session == null)
            {
                logger.Error(this.ClassName(), this.MethodName(), "Session Object is null!!!");
                return;
            }

            session.Closed -= OnSessionClosed;

            session.ClearAllSocketAsyncEvent();

            mRecvEventPoolFix.Return(session.mRecvEvent);
            mSendEventPoolFix.Return(session.mSendEvent);

            Close(session.mSessionID);
        }
        */
    }


    
}
