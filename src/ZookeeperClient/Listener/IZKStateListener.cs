using System;
using System.Threading.Tasks;
using static org.apache.zookeeper.Watcher.Event;

namespace ZookeeperClient.Listener
{
    public interface IZKStateListener
    {
        /// <summary>
        /// 状态改变的回调函数
        /// state
        /// </summary>
        Func<KeeperState, Task> StateChangedHandler { set; get; }

        /// <summary>
        /// 会话创建的回调函数
        /// any ephemeral nodes here.
        /// </summary>
        Func<Task> NewSessionHandler { set; get; }

        /// <summary>
        ///会话出错的回调函数
        /// failure handling e.g.retry to connect or pass the error up
        /// error
        /// </summary>
        Func<Exception, Task> SessionEstablishmentErrorHandler { set; get; }
    }
}
