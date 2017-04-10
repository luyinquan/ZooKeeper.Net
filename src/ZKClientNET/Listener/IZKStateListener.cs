using System;
using ZooKeeperNet;

namespace ZKClientNET.Listener
{
    public interface IZKStateListener
    {
        /// <summary>
        /// 状态改变的回调函数
        /// </summary>
        /// <param name="state"></param>
        void HandleStateChanged(KeeperState state);

        /// <summary>
        /// 会话创建的回调函数
        /// any ephemeral nodes here.
        /// </summary>
        void HandleNewSession();

        /// <summary>
        ///会话出错的回调函数
        /// failure handling e.g.retry to connect or pass the error up
        /// </summary>
        /// <param name="error"></param>
        void HandleSessionEstablishmentError(Exception error);

    }
}
