using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Exceptions;
using ZKClientNET.Listener;
using ZKClientNET.Lock;
using ZKClientNET.Scheduling;
using ZooKeeperNet;

namespace ZKClientNET.Leader
{
    /// <summary>
    /// 选举Leader
    /// </summary>
    public class ZKLeaderDelySelector : ILeaderSelector
    {
        private string id;
        private ZKClient client;
        private int delayTimeMillis = 5000;
        private ZKDistributedDelayLock _lock;
        private string leaderPath;
        private CancellationTokenSource cancellationTokenSource;
        private TaskFactory factory;
        private ZKLeaderSelectorListener listener;
        private ZKStateListener stateListener;
        private int isInterrupted = 0;
        private int autoRequeue = 0;
        private int state = (int)State.LATENT;
        private string curentNodePath;

        private enum State
        {
            LATENT,
            STARTED,
            CLOSED
        }

        /// <summary>
        /// 创建Leader选举对象
        /// </summary>
        /// <param name="id">每个Leader选举的参与者都有一个ID标识，用于区分各个参与者。</param>
        /// <param name="autoRequue">是否在由于网络问题造成与服务器断开连接后，自动参与到选举队列中。</param>
        /// <param name="delayTimeMillis">延迟选举的时间，主要是针对网络闪断的情况，给Leader以重连并继续成为Leader的机会，一般5秒合适。</param>
        /// <param name="client">ZKClient</param>
        /// <param name="leaderPath">选举的路径</param>
        /// <param name="listener">成为Leader后执行的的监听器</param>
        public ZKLeaderDelySelector(string id, int autoRequue, int delayTimeMillis, ZKClient client, string leaderPath, ZKLeaderSelectorListener listener)
        {
            this.delayTimeMillis = delayTimeMillis;
            this.id = id;
            this.client = client;
            this.autoRequeue = autoRequue;
            this.leaderPath = leaderPath;
            this._lock = ZKDistributedDelayLock.NewInstance(client, leaderPath);
            this._lock.SetLockNodeData(this.id);
            this.listener = listener;
            SetFactory();

            stateListener = new ZKStateListener().StateChanged(
            (state) =>
            {
                if (state == KeeperState.SyncConnected)
                {
                    //如果重新连接
                    if (isInterrupted == 0)
                    {
                        Requeue();
                    }
                }
            });

        }

        private void SetFactory()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.factory = new TaskFactory(
               cancellationTokenSource.Token,
               TaskCreationOptions.None,
               TaskContinuationOptions.None,
               new LimitedConcurrencyLevelTaskScheduler(1));
        }

        /// <summary>
        /// 启动参与选举Leader
        /// </summary>
        public void Start()
        {
            if ((int)State.LATENT != Interlocked.CompareExchange(ref state, (int)State.LATENT, (int)State.STARTED))
            {
                throw new ZKException("Cannot be started more than once");
            }
            client.SubscribeStateChanges(stateListener);
            Requeue();
        }

        /// <summary>
        /// 重新添加当前线程到选举队列
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Requeue()
        {
            if (state != (int)State.STARTED)
            {
                throw new ZKException("close() has already been called");
            }

            Interlocked.Exchange(ref isInterrupted, 0);
            cancellationTokenSource.Cancel();
            SetFactory();

            //添加到参与者节点中
            AddParticipantNode();

            factory.StartNew(() =>
            {
                if (!factory.CancellationToken.IsCancellationRequested)
                {
                    _lock.Lock(0, delayTimeMillis);
                    listener.TakeLeadership(client, this);
                }
            });

        }

        /// <summary>
        /// 获得Leader ID
        /// </summary>
        /// <returns></returns>
        public string GetLeader()
        {
            return client.ReadData<string>(_lock.GetLockPath());
        }

        /// <summary>
        /// 是否是Leader
        /// </summary>
        /// <returns></returns>
        public bool IsLeader()
        {
            return _lock.hasLock == 1;
        }

        /// <summary>
        ///  获得当前的所有参与者的路径名
        /// </summary>
        /// <returns></returns>
        public List<string> GetParticipantNodes()
        {
            return client.GetChildren(leaderPath + "/nodes");
        }

        private void AddParticipantNode()
        {
            string path = client.Create(leaderPath + "/nodes/1", id, CreateMode.EphemeralSequential);
            curentNodePath = path;
        }

        private void RemoveParticipantNode()
        {
            client.Delete(curentNodePath);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void InterruptLeadership()
        {
            cancellationTokenSource.Cancel();
            SetFactory();
            Interlocked.CompareExchange(ref isInterrupted, 0, 1);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Close()
        {
            if ((int)State.STARTED != Interlocked.CompareExchange(ref state, (int)State.STARTED, (int)State.CLOSED))
            {
                throw new ZKException("Already closed or has not been started");
            }
            _lock.UnLock();
            //从参与者节点列表中移除
            RemoveParticipantNode();
            client.UnSubscribeStateChanges(stateListener);
            cancellationTokenSource.Cancel();
        }
    }
}
