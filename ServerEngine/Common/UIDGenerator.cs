using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerEngine.Config;

namespace ServerEngine.Common
{
    public static class UUIDGenerator
    {
        public static string Get => Guid.NewGuid().ToString();
    }

    public class UIDGenerator
    {
        // Todo: config 와 같이 외부에서 설정할 수 있도록 수정
        // UID가 생성된 장소
        public enum eCreateType
        {
            None = 0,       
            Source = 1,         // 소스 코드에서 생성
            Tool = 2,           // 툴에서 생성
            Other = 3           // 기타 외부에서 생성
        }

        // UID가 사용될 콘텐츠 타입
        public enum eContentsType
        {
            None = 0,
            UserToken = 1,
            Item = 2,
            Max = 3
        }

        private int mServerGid;
        private int mServerIndex;
        private eContentsType mContentsType;
        private eCreateType mCreateType;    
        private int mLoopCount = 0;

        private readonly uint mLoopLimit;

        public UIDGenerator(eContentsType type, int server_gid, int server_index, uint loop_limit)
        {
            mServerGid = server_gid;
            mServerIndex = server_index;

            // loop_limit는 콘텐츠 타입에 따라서 값이 달라진다. (ex:UserToken >> 4자리, Item >> 8자리...)
            mLoopLimit = loop_limit;

            mContentsType = type;
            mCreateType = eCreateType.Source;
        }

        /// <summary>
        /// 콘텐츠 타입에 따라서 생성된 UID 반환
        /// - 자릿수가 20자리를 넘어가므로 string 타입으로만 사용해야된다
        /// - 생성 규칙을 반드시 지켜야 한다
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            string uid = string.Empty;
            int loop_count = (Interlocked.Increment(ref mLoopCount) % (int)mLoopLimit) + 1;             // 0번 index는 사용하지 않는다

            var create_time = DateTime.UtcNow.ToUnixTimeUInt64();

            // UID 규칙 : 생성장소 + 서버아이디 + 서버 인덱스 + UID 콘텐츠 타입 + 생성시간 + 전역 카운터 
            // 4 + 2 + 2 + 10 + 5 + 1 => 24
            // ex) 2011 11 10 1724863307 0001 1 (23자리)
            var contents_type = ((int)mContentsType).ToString("D2");
            switch (mContentsType)
            {
                case eContentsType.UserToken:
                    {
                        // 23
                        uid = $"{(int)mCreateType:D1}{mServerGid:D4}{mServerIndex:D2}{contents_type}{create_time}{loop_count.ToString("D4")}";
                    }
                    break;
                case eContentsType.Item:
                    {
                        // 27
                        uid = $"{(int)mCreateType:D1}{mServerGid:D4}{mServerIndex:D2}{contents_type}{create_time}{loop_count.ToString("D8")}";
                    }
                    break;
            }

            return uid;
        }
    }
}
