using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerEngine.Common
{
    /// <summary>
    /// 전역에서 사용되는 공통적인 기능 구현 클래스
    /// </summary>
    public static class GlobalFunction
    {
        /// <summary>
        /// 현재 서버에서 통신에 사용되는 프로토콜의 타입 반환 
        /// * 기본값은 TCP
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public static eProtocolType GetProtocolType(string _type)
        {
            var type = _type.ToLower().Trim();
            eProtocolType result = eProtocolType.UNKNOWN;
            switch (type)
            {
                case "tcp":
                    result = eProtocolType.TCP; 
                    break;
                case "udp":
                    result = eProtocolType.UDP;
                    break;
                case "http":
                    result = eProtocolType.HTTP;
                    break;              
                default:
                    result = eProtocolType.TCP;
                    break;
            }
            return result;
        }
    }
}
