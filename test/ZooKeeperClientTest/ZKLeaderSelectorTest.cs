using log4net;
using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZooKeeperClient.Client;
using ZooKeeperClient.Leader;
using ZooKeeperClient.Util;

namespace ZooKeeperClient.Test
{
    public class ZKLeaderSelectorTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKLeaderSelectorTest));
        private ZKClient _zkClient;
        private string leaderPath = "/zk/leader";

        /// <summary>
        /// 测试分布式锁
        /// </summary>
       [Fact]
        public async Task TestZKLeaderSeletor()
        {
            LOG.Info("------------ BEFORE -------------");
            using (_zkClient = new ZKClient(TestUtil.zkServers))
            {
                await TestUtil.ReSetPathUnCreate(_zkClient, leaderPath);

                var msgList = new List<string>();
                var count = new CountdownEvent(20);
                var count1 = new CountdownEvent(20);
                await _zkClient.CreateRecursiveAsync(leaderPath, null, CreateMode.PERSISTENT);
                var index = 0;
                for (int i = 0; i < 20; i++)
                {
                    var name = "server:" + index;
                    var task = new Task(() =>
                       {
                           var zkClient1 = new ZKClient(TestUtil.zkServers);
                           var listener = new ZKLeaderSelectorListener();
                           listener.takeLeadership = async (client, selector) =>
                                     {
                                         msgList.Add(name + " I am the leader");
                                         Console.WriteLine(name + ": I am the leader-" + await selector.GetLeaderAsync());
                                         selector.Close();
                                         count1.Signal();
                                     };

                           var _selector = new ZKLeaderSelector(name, true, zkClient1, leaderPath, listener);
                           try
                           {
                               Console.WriteLine(name + ":waiting");
                               count.Wait();
                           }
                           //catch (ThreadInterruptedException)
                           catch (Exception)
                           {
                           }
                           _selector.Start();

                           try
                           {
                               count1.Wait();
                           }
                           //catch (ThreadInterruptedException)
                           catch (Exception)
                           {
                           }

                       });
                    task.Start();
                    count.Signal();
                    Interlocked.Increment(ref index);
                }

                int size = TestUtil.WaitUntil(20, () => { return msgList.Count; }, new TimeSpan(0, 0, 100));
                Assert.True(size == 20);
            }
            LOG.Info("------------ AFTER -------------");
        }

       [Fact]
        public async Task TestZKLeaderSeletor1()
        {
            LOG.Info("------------ BEFORE -------------");
            using (_zkClient = new ZKClient(TestUtil.zkServers))
            {
                await TestUtil.ReSetPathUnCreate(_zkClient, leaderPath);

                var msgList = new List<string>();
                await _zkClient.CreateRecursiveAsync(leaderPath, null, CreateMode.PERSISTENT);

                var listener = new ZKLeaderSelectorListener();
                listener.takeLeadership = async (client, selector) =>
                     {
                         try
                         {
                             await Task.Delay(1000);
                         }
                         //catch (ThreadInterruptedException)
                         catch (Exception)
                         {
                         }
                         msgList.Add("server1 I am the leader");
                         Console.WriteLine("server1: I am the leader-" + await selector.GetLeaderAsync());
                     };


                var selector1 = new ZKLeaderSelector("server1", true, _zkClient, leaderPath, listener);

                var task = new Task(() =>
                 {
                     var zkClient1 = ZKClientBuilder.NewZKClient()
                                .Servers(TestUtil.zkServers)
                                .SessionTimeout(10000)
                                .Build();
                     var listener2 = new ZKLeaderSelectorListener();

                     listener2.takeLeadership = async (client, selector) =>
                     {
                         try
                         {
                             await Task.Delay(1000);
                         }
                         //catch (ThreadInterruptedException)
                         catch (Exception)
                         {
                         }
                         msgList.Add("server2 I am the leader");
                         Console.WriteLine("server2: I am the leader-" + await selector.GetLeaderAsync());
                         selector.Close();
                     };
                     var selector2 = new ZKLeaderSelector("server2", true, zkClient1, leaderPath, listener2);
                     selector2.Start();
                 });

                selector1.Start();
                task.Start();
                Thread.Sleep(1000);

                int size = TestUtil.WaitUntil(2, () => { return msgList.Count; }, new TimeSpan(0, 0, 100));
                Assert.True(size == 2);
            }
        }

    }
}
