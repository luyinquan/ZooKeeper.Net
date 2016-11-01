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
    public class ZKLeaderSelector : ILeaderSelector
    {

        private string id;
        private ZKClient client;
        private ZKDistributedLock _lock;
        private string leaderPath;
        private CancellationTokenSource cancellationTokenSource;
        private TaskFactory factory;
        private ZKLeaderSelectorListener listener;
        private ZKStateListener stateListener;
        private int isInterrupted = 0;
        private int autoRequeue = 0;
        private Task<object> ourTask = null;
        private int state = (int)State.LATENT;

        private enum State
        {
            LATENT = 0,
            STARTED = 1,
            CLOSED = 2
        }

        /// <summary>
        /// 创建Leader选举对象
        /// </summary>
        /// <param name="id"> 每个Leader选举的参与者都有一个ID标识，用于区分各个参与者。</param>
        /// <param name="autoRequue"> 是否在由于网络问题造成与服务器断开连接后，自动参与到选举队列中。</param>
        /// <param name="client"> ZKClient</param>
        /// <param name="leaderPath"> 选举的路径</param>
        /// <param name="listener"> 成为Leader后执行的的监听器</param>
        public ZKLeaderSelector(string id, int autoRequue, ZKClient client, string leaderPath, ZKLeaderSelectorListener listener)
        {
            this.id = id;
            this.client = client;
            this.autoRequeue = autoRequue;
            this.leaderPath = leaderPath;
            this._lock = ZKDistributedLock.NewInstance(client, leaderPath);
            this._lock.lockNodeData = id;
            this.listener = listener;
            SetFactory();
            this.stateListener = new ZKStateListener().StateChanged(
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
            factory.StartNew(() =>
            {
                if (!factory.CancellationToken.IsCancellationRequested)
                {
                    _lock.Lock();
                    listener.TakeLeadership(client, this);
                }
            });
            
        }

        /// <summary>
        /// 获得
        /// </summary>
        /// <returns></returns>
        public string GetLeader()
        {
            if (_lock.GetParticipantNodes().Count > 0)
            {
                return client.ReadData<string>(leaderPath + "/" + _lock.GetParticipantNodes()[0]);
            }
            return null;
        }

        public bool IsLeader()
        {
            if (client.GetCurrentState() == KeeperState.SyncConnected)
            {
                if (_lock.GetParticipantNodes().Count > 0)
                {
                    return id == client.ReadData<string>(leaderPath + "/" + _lock.GetParticipantNodes()[0]);
                }
            }

            return false;
        }

        public List<string> GetParticipantNodes()
        {
            return _lock.GetParticipantNodes();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void InterruptLeadership()
        {
            cancellationTokenSource.Cancel();
            SetFactory();
            Interlocked.CompareExchange(ref isInterrupted, 0, 1);
        }

        /// <summary>
        /// 关闭Leader选举
        /// </summary>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Close()
        {
            if ((int)State.STARTED != Interlocked.CompareExchange(ref state, (int)State.STARTED, (int)State.CLOSED))
            {
                throw new ZKException("Already closed or has not been started");
            }
            _lock.UnLock();
            client.UnSubscribeStateChanges(stateListener);
            cancellationTokenSource.Cancel();
        }
        
    }
}
