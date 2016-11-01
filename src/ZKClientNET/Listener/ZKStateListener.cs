using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZKClientNET.Listener
{
    public class ZKStateListener : IZKStateListener
    {
        private event Action<KeeperState> stateChangedEvent;

        private event Action newSessionEvent;

        private event Action<Exception> sessionEstablishmentErrorEvent;

        public ZKStateListener StateChanged(Action<KeeperState> stateChanged)
        {
            stateChangedEvent += stateChanged;
            return this;
        }

        public ZKStateListener NewSession(Action newSession)
        {
            newSessionEvent += newSession;
            return this;
        }

        public ZKStateListener SessionEstablishmentError(Action<Exception> sessionEstablishmentError)
        {
            sessionEstablishmentError += sessionEstablishmentError;
            return this;
        }


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
