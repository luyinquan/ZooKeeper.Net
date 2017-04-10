using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Leader;
using ZKClientNETTest.Util;
using ZooKeeperNet;

namespace ZKClientNETTest.Test
{
    public class ZKLeaderSelectorTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKLeaderSelectorTest));
        private ZKClient _zkClient;
        private string leaderPath = "/zk/leader";

        [OneTimeSetUp]
        public virtual void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = ZKClientBuilder.NewZKClient()
                             .Servers(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port))
                             .SessionTimeout(10000)
                             .Build();
            TestUtil.ReSetPathUnCreate(_zkClient, leaderPath);
        }

        [OneTimeTearDown]
        public virtual void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        /// <summary>
        /// 测试分布式锁
        /// </summary>
        [Test]
        public void TestZKLeaderSeletor()
        {
            List<string> msgList = new List<string>();
            CountdownEvent count = new CountdownEvent(20);
            CountdownEvent count1 = new CountdownEvent(20);
            _zkClient.CreateRecursive(leaderPath, null, CreateMode.Persistent);
            int index = 0;
            for (int i = 0; i < 20; i++)
            {
                string name = "server:" + index;
                Task task = new Task(() =>
                   {
                       ZKClient zkClient1 = ZKClientBuilder.NewZKClient()
                                .Servers(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port))
                                .SessionTimeout(10000)
                                .Build();
                       ZKLeaderSelectorListener listener = new ZKLeaderSelectorListener()
                                   .TakeLeadership((client, selector) =>
                                   {
                                       msgList.Add(name + " I am the leader");
                                       Console.WriteLine(name + ": I am the leader-" + selector.GetLeader());
                                       selector.Close();
                                       count1.Signal();
                                   });


                       ZKLeaderSelector _selector = new ZKLeaderSelector(name, true, zkClient1, leaderPath, listener);

                       try
                       {
                           Console.WriteLine(name + ":waiting");
                           count.Wait();
                       }
                       catch (ThreadInterruptedException e1)
                       {
                       }
                       _selector.Start();

                       try
                       {
                           count1.Wait();
                       }
                       catch (ThreadInterruptedException e)
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

        [Test]
        public void TestZKLeaderSeletor1()
        {
            List<string> msgList = new List<string>();
            _zkClient.CreateRecursive(leaderPath, null, CreateMode.Persistent);

            ZKLeaderSelectorListener listener = new ZKLeaderSelectorListener()
                .TakeLeadership((client, selector) =>
                {
                    try
                    {
                        Thread.Sleep(1000);
                    }
                    catch (ThreadInterruptedException e)
                    {
                    }
                    msgList.Add("server1 I am the leader");
                    Console.WriteLine("server1: I am the leader-" + selector.GetLeader());
                });


            ZKLeaderSelector selector1 = new ZKLeaderSelector("server1", true, _zkClient, leaderPath, listener);

            Task task = new Task(() =>
             {
                 ZKClient zkClient1 = ZKClientBuilder.NewZKClient()
                            .Servers(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port))
                            .SessionTimeout(10000)
                            .Build();
                 ZKLeaderSelectorListener listener2 = new ZKLeaderSelectorListener()
                .TakeLeadership((client, selector) =>
                {
                    try
                    {
                        Thread.Sleep(1000);
                    }
                    catch (ThreadInterruptedException e)
                    {
                    }
                    msgList.Add("server2 I am the leader");
                    Console.WriteLine("server2: I am the leader-" + selector.GetLeader());
                    selector.Close();
                });
                 ZKLeaderSelector selector2 = new ZKLeaderSelector("server2", true, zkClient1, leaderPath, listener2);
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
