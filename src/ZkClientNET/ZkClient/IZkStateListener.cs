using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZkClientNET.ZkClient
{
    public interface IZkStateListener
    {

        /// <summary>
        /// Called when the zookeeper connection state has changed.
        /// </summary>
        /// <param name="state"></param>
        void handleStateChanged(KeeperState state);

        /// <summary>
        ///  Called after the zookeeper session has expired and a new session has been created. You would have to re-create
        /// any ephemeral nodes here.
        /// </summary>
        void HandleNewSession();

        /// <summary>
        /// Called when a session cannot be re-established. This should be used to implement connection
        /// failure handling e.g.retry to connect or pass the error up
        /// </summary>
        /// <param name="error"></param>
        void HandleSessionEstablishmentError(Exception error);
    }
}
