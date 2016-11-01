using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Leader;
using ZKClientNETTest.Util;
using ZooKeeperNet;

namespace ZKClientNETTest.Test
{
  
   public  class ZKLeaderDelaySelectorTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKLeaderDelaySelectorTest));
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
            TestUtil.ReSetPathCreate(_zkClient, leaderPath);
        }

        [OneTimeTearDown]
        public virtual void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        public void TestZKLeaderDelaySeletor()
        {
            List<string> msgList = new List<string>();
            //CountDownLatch latch = new CountDownLatch(5);
            //CountDownLatch latch1 = new CountDownLatch(5);
            _zkClient.CreateRecursive(leaderPath, null, CreateMode.Persistent);
            int index = 0;

            for (int i = 0; i < 5; i++)
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
                                                        Console.WriteLine(selector.GetParticipantNodes());
                                                        try
                                                        {
                                                            Thread.Sleep(100);
                                                        }
                                                        catch (ThreadInterruptedException e)
                                                        {
                                                        }
                                                        selector.Close();
                                                        //latch1.countDown();
                                                    });
                   

                    ZKLeaderDelySelector selector1 = new ZKLeaderDelySelector(name, 1, 3000, zkClient1, leaderPath, listener);

                    //try
                    //{
                    //    Console.WriteLine(name + ":waiting");
                    //    latch.await();
                    //}
                    //catch (InterruptedException e1)
                    //{
                    //    e1.printStackTrace();
                    //}
                    selector1.Start();

                    //try
                    //{
                    //    latch1.await();
                    //}
                    //catch (ThreadInterruptedException e)
                    //{                      
                    //}

                });
                task.Start();
                //latch.countDown();
                Interlocked.Increment(ref index);
            }

            int size = TestUtil.WaitUntil(5, () => { return msgList.Count; }, new TimeSpan(0, 0, 100));
            Assert.True(size == 5);
        }

        [Test]
        public void TestZKLeaderDelaySeletor1()
        {
            List<string> msgList = new List<string>();
            _zkClient.CreateRecursive(leaderPath, null, CreateMode.Persistent);

            ZKLeaderSelectorListener listener1 = new ZKLeaderSelectorListener()
                    .TakeLeadership((client, selector) =>
                    {
                        msgList.Add("server1 I am the leader");
                        try
                        {
                            Thread.Sleep(1000);
                        }
                        catch (ThreadInterruptedException e)
                        {
                        }

                        Console.WriteLine("server1: I am the leader-" + selector.GetLeader());
                        // client.reconnect();
                    });
        

            ZKLeaderDelySelector selector1 = new ZKLeaderDelySelector("server1", 1, 3000, _zkClient, leaderPath, listener1);

            Task task = new Task(() =>
            {
                ZKClient zkClient1 = ZKClientBuilder.NewZKClient()
                           .Servers(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port))
                           .SessionTimeout(10000)
                           .Build();

                ZKLeaderSelectorListener listener2 = new ZKLeaderSelectorListener()
                  .TakeLeadership((client, selector) =>
                  {
                      msgList.Add("server2 I am the leader");
                      try
                      {
                          Thread.Sleep(1000);
                      }
                      catch (ThreadInterruptedException e)
                      {
                      }

                      Console.WriteLine("server2: I am the leader-" + selector.GetLeader());
                      selector.Close();
                  });
              
                ZKLeaderDelySelector selector2 = new ZKLeaderDelySelector("server2", 1, 3000, zkClient1, leaderPath, listener2);
                selector2.Start();
            });
            selector1.Start();
            task.Start();

            int size = TestUtil.WaitUntil(1, () => { return msgList.Count; }, new TimeSpan(0, 0, 100));
            Assert.True(size == 1);
        }

        [Test]
        public void TestZKLeaderDelaySeletor2()
        {
            List<string> msgList = new List<string>();
            _zkClient.CreateRecursive(leaderPath, null, CreateMode.Persistent);

            ZKLeaderSelectorListener listener1 = new ZKLeaderSelectorListener()
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
                    // client.reconnect();
                });
          

            ZKLeaderDelySelector selector1 = new ZKLeaderDelySelector("server1", 1, 1, _zkClient, leaderPath, listener1);

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

                ZKLeaderDelySelector selector2 = new ZKLeaderDelySelector("server2", 1, 1, zkClient1, leaderPath, listener2);
                selector2.Start();
            });
            selector1.Start();
            task.Start();

            int size = TestUtil.WaitUntil(3, () => { return msgList.Count; }, new TimeSpan(0, 0, 100));
            Assert.True(size == 3);
        }
    }
}
