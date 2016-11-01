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
using ZKClientNET.Scheduling;
using ZKClientNET.Util;
using ZooKeeperNet;

namespace ZKClientNET.Lock
{
    /// <summary>
    /// 带延迟获取的分布式锁
    /// 此分布式锁主要针对网络闪断的情况。
    /// 不带延迟功能的分布式锁：某个线程获取了分布式锁,在网络发生闪断，ZooKeeper删除了临时节点，那么就会释放锁。
    /// 带延迟功能的分布式锁：例如设置了delayTimeMillis的值为5000，那么在发生网络闪断ZooKeeper删除了临时节点后5秒内重新连上。
    /// 非线程安全，每个线程请单独创建实例
    /// </summary>
    public class ZKDistributedDelayLock : IZKLock
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZKDistributedDelayLock));
        private CancellationTokenSource cancellationTokenSource;
        private TaskFactory factory;
        private IZKDataListener nodeListener;
        private IZKStateListener stateListener;
        private ZKClient client;
        private string lockPath;
        private Semaphore semaphore;
        public int hasLock = 0;
        //锁的值一定要唯一,且不允许为null，这里采用Guid
        private string lockNodeData = Guid.NewGuid().ToString("N");
        private  int delayTimeMillis = 0;

        public ZKDistributedDelayLock(ZKClient client, string lockPach)
        {
            this.client = client;
            this.lockPath = lockPach;
            cancellationTokenSource = new CancellationTokenSource();
            factory = new TaskFactory(
                cancellationTokenSource.Token, 
                TaskCreationOptions.None,
                TaskContinuationOptions.None, 
                new LimitedConcurrencyLevelTaskScheduler(1));

            nodeListener = new ZKDataListener().DataDeleted(
            (path) =>
            {
                factory.StartNew(() =>
                {
                    if (!factory.CancellationToken.IsCancellationRequested)
                    {
                        if (0 != Interlocked.Exchange(ref hasLock, 1))
                        {
                            //如果当前没有持有锁
                            //为了解决网络闪断问题，先等待一段时间，再重新竞争锁
                            Thread.Sleep(delayTimeMillis);
                            //如果之前获得锁的线程解除了锁定，则所有等待的线程都重新尝试，这里使得信号量加1
                            semaphore.Release();
                        }
                    }
                }, factory.CancellationToken);
            });

            stateListener = new ZKStateListener().StateChanged((state) =>
            {
                if (state == KeeperState.SyncConnected)
                {
                    //如果重新连接
                    factory.StartNew(() =>
                    {
                        if (!factory.CancellationToken.IsCancellationRequested)
                        {
                            if (0 == Interlocked.Exchange(ref hasLock, 1))
                            {
                                //现在持有锁                            
                                //重新创建节点
                                try
                                {
                                    client.Create(lockPath + "/lock", lockNodeData, CreateMode.Ephemeral);
                                }
                                catch (ZKNodeExistsException e)
                                {
                                    try
                                    {
                                        if (lockNodeData != client.ReadData<string>(lockPath + "/lock"))
                                        {
                                            Interlocked.Exchange(ref hasLock, 0);
                                        }
                                    }
                                    catch (ZKNoNodeException e2)
                                    {
                                        //ignore
                                    }
                                }
                            }
                        }
                    }, factory.CancellationToken);
                }
            });


        }

        public static ZKDistributedDelayLock NewInstance(ZKClient client, string lockPach)
        {
            if (!client.Exists(lockPach))
            {
                throw new ZKNoNodeException("The lockPath is not exists!,please create the node.[path:" + lockPach + "]");
            }
            ZKDistributedDelayLock zkDistributedDelayLock = new ZKDistributedDelayLock(client, lockPach);
            client.SubscribeDataChanges(lockPach + "/lock", zkDistributedDelayLock.nodeListener);
            client.SubscribeStateChanges(zkDistributedDelayLock.stateListener);
            try
            {
                client.Create(lockPach + "/nodes", null, CreateMode.Persistent);
            }
            catch (ZKNodeExistsException e)
            {
                //已被其他线程创建，这里忽略就可以
            }

            return zkDistributedDelayLock;
        }

        public bool Lock()
        {
            return Lock(0);
        }

        /// <summary>
        /// 获得锁,默认的延迟时间5000毫秒
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns>
        /// 超时时间
        /// 如果超时间大于0，则会在超时后直接返回false。
        /// 如果超时时间小于等于0，则会等待直到获取锁为止。
        /// </returns>
        public bool Lock(int timeout)
        {
            return Lock(timeout, 5000);
        }

        /// <summary>
        /// 获得锁路径
        /// </summary>
        /// <returns></returns>
        public string GetLockPath()
        {
            return lockPath + "/lock";
        }

        public bool Lock(int timeout, int delayTimeMillis)
        {
            this.delayTimeMillis = delayTimeMillis;
            long startTime = DateTime.Now.ToUnixTime();
            while (true)
            {
                try
                {
                    semaphore = new Semaphore(1, 1);
                    client.Create(lockPath + "/lock", lockNodeData, CreateMode.Ephemeral);
                    Interlocked.CompareExchange(ref hasLock, 0, 1);
                    return true;
                }
                catch (ZKNodeExistsException e)
                {
                    try
                    {
                        semaphore.WaitOne();
                    }
                    catch (ThreadInterruptedException interruptedException)
                    {
                        return false;
                    }
                }
                //超时处理
                if (timeout > 0 && (DateTime.Now.ToUnixTime() - startTime) >= timeout)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// 设置锁存储的值，一定要唯一，且不允许为null
        /// 默认使用UUID动态生成
        /// </summary>
        /// <param name="lockNodeData"></param>
        public void SetLockNodeData(string lockNodeData)
        {
            if (string.IsNullOrEmpty(lockNodeData))
            {
                throw new ZKException("lockNodeData can not be null!");
            }
            this.lockNodeData = lockNodeData;
        }

        public bool UnLock()
        {
            if (0 == Interlocked.Exchange(ref hasLock, 1))
            {
                Interlocked.CompareExchange(ref hasLock, 1, 0);
                client.UnSubscribeDataChanges(lockPath + "/lock", nodeListener);
                client.UnSubscribeStateChanges(stateListener);
                cancellationTokenSource.Cancel();
                bool flag = client.Delete(lockPath + "/lock");
                return flag;            
            }
            throw new ZKException("not locked can not unlock!");
        }

    }
}
