using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Exceptions;
using ZKClientNET.Listener;
using ZooKeeperNet;

namespace ZKClientNET.Lock
{
    /// <summary>
    /// 主从服务锁，主服务一直持有锁，断开连接，从服务获得锁
    /// 非线程安全，每个线程请单独创建实例
    /// </summary>
    public class ZKHALock : IZKLock
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZKHALock));

        private IZKChildListener countListener;
        private IZKStateListener stateListener;
        private ZKClient client;
        private string lockPath;
        private string currentSeq;
        private Semaphore semaphore;

        private ZKHALock(ZKClient client, string lockPach)
        {
            this.client = client;
            this.lockPath = lockPach;
            countListener = new ZKChildListener().ChildChange(
             (parentPath, currentChilds) =>
            {
                if (Check(currentSeq, currentChilds))
                {
                    semaphore.Release();
                }
            });
            stateListener = new ZKStateListener().StateChanged(
            (state) =>
            {
                if (state == KeeperState.SyncConnected)
                {
                    //如果重新连接
                    //如果重连后之前的节点已删除，并且lock处于等待状态，则重新创建节点，等待获得lock
                    if (!client.Exists(lockPach + "/" + currentSeq) && !semaphore.WaitOne(1000))
                    {
                        string newPath = client.Create(lockPath + "/1", null, CreateMode.EphemeralSequential);
                        string[] paths = newPath.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                        currentSeq = paths[paths.Length - 1];
                    }
                }
            });
        }

        public static ZKHALock NewInstance(ZKClient client, string lockPach)
        {
            if (!client.Exists(lockPach))
            {
                throw new ZKNoNodeException("The lockPath is not exists!,please create the node.[path:" + lockPach + "]");
            }

            ZKHALock zkhaLock = new ZKHALock(client, lockPach);
            //对lockPath进行子节点数量的监听
            client.SubscribeChildChanges(lockPach, zkhaLock.countListener);
            //对客户端连接状态进行监听
            client.SubscribeStateChanges(zkhaLock.stateListener);
            return zkhaLock;
        }

        public bool Lock()
        {
            semaphore = new Semaphore(1, 1);
            string newPath = client.Create(lockPath + "/1", null, CreateMode.EphemeralSequential);
            string[] paths = newPath.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            currentSeq = paths[paths.Length - 1];
            bool getLock = false;
            try
            {
                semaphore.WaitOne();
                getLock = true;
            }
            catch (ThreadInterruptedException e)
            {
                throw new ZKInterruptedException(e);
            }
            if (getLock)
            {
                LOG.Debug("get halock successful.");
            }
            else
            {
                LOG.Debug("failed to get halock.");
            }
            return getLock;
        }

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <returns>
        /// 如果释放锁成功返回true，否则返回false
        /// 释放锁失败只有一种情况，就是线程正好获得锁，在释放之前，
        /// 与服务器断开连接，并且会话过期，这时候服务器会自动删除EphemeralSequential节点。
        ///在会话过期之后再删除节点就会删除失败，因为路径已经不存在了。
        /// </returns>
        public bool UnLock()
        {
            return client.Delete(lockPath + "/" + currentSeq);
        }

        private bool Check(string checkPath, List<string> children)
        {
            if (children == null || !children.Contains(checkPath))
            {
                return false;
            }
            //判断checkPath 是否是children中的最小值，如果是返回true，不是返回false
            long chePathSeq = long.Parse(checkPath);
            bool isLock = true;
            foreach (string path in children)
            {
                long pathSeq = long.Parse(path);
                if (chePathSeq > pathSeq)
                {
                    isLock = false;
                    break;
                }
            }
            return isLock;
        }
    }
}
