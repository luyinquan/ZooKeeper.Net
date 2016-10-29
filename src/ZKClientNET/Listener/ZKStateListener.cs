using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZKClientNET.Listener
{
    public class ZKStateListener
    {
        public event Action<KeeperState> stateChangedEvent;

        public event Action newSessionEvent;

        public event Action<Exception> sessionEstablishmentErrorEvent;

        /// <summary>
        /// 状态改变的回调函数
        /// </summary>
        /// <param name="state"></param>
        public void HandleStateChanged(KeeperState state)
        {
            stateChangedEvent(state);
        }

        /// <summary>
        /// 会话创建的回调函数
        /// any ephemeral nodes here.
        /// </summary>
        public void HandleNewSession()
        {
            newSessionEvent();
        }

        /// <summary>
        ///会话出错的回调函数
        /// failure handling e.g.retry to connect or pass the error up
        /// </summary>
        /// <param name="error"></param>
        public void HandleSessionEstablishmentError(Exception error)
        {
            sessionEstablishmentErrorEvent(error);
        }

    }
}
