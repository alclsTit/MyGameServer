using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ServerEngine.Network.SystemLib;
using ServerEngine.Config;

namespace RelayServer.Config
{
    /// <summary>
    /// 프로세스당 config 옵션파일 로더는 한개뿐이므로 싱클턴 패턴 적용
    /// </summary>
    public sealed class ConfigLoader : ConfigLoaderBase<ConfigLoader, RelayServerConfig>
    {
        private ConfigLoader()
        {
            // 싱글톤 객체이므로 생성자는 외부에서 접근하지못하도록 접근제한자를 private로 설정해야만 한다         
        }

        /// <summary>
        /// config 파일로부터 서버 옵션 세팅
        /// *[ServerInfo] 섹션에 해당되는 서버 공통옵션은 디폴트로 세팅되어있는 상태
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public override bool LoadConfig(out RelayServerConfig item)
        {
            if (string.IsNullOrEmpty(mFilePath))
            {
                item = null;
                throw new Exception($"{nameof(mFilePath)} is Error!!!");
            }

            if (!base.LoadConfig(out item))
                return false;

            // Todo: 기타 서버옵션 세팅

            return true;
        }

        /// <summary>
        /// config 파일로부터 서버 옵션 중 리스너 옵션세팅
        /// </summary>
        /// <param name="listeners"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public override bool LoadListeners(List<IListenInfo> listeners)
        {
            if (string.IsNullOrEmpty(mFilePath))
                throw new Exception($"{nameof(mFilePath)} is Error!!!");

            if (listeners == null)
                throw new ArgumentNullException(nameof(listeners));

            if (!base.LoadListeners(listeners))
                return false;

            return true;
        }
    }
}
