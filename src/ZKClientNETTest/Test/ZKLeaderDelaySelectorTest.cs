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
            TestUtil.ReSetPathUnCreate(_zkClient, leaderPath);
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
            CountdownEvent count = new CountdownEvent(5);
            CountdownEvent count1 = new CountdownEvent(5);
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
                                                        Console.WriteLine(string.Join(".", selector.GetParticipantNodes()));
                                                        try
                                                        {
                                                            Thread.Sleep(100);
                                                        }
                                                        catch (ThreadInterruptedException e)
                                                        {
                                                        }
                                                        selector.Close();
                                                        count1.Signal();
                                                    });
                   

                    ZKLeaderDelySelector selector1 = new ZKLeaderDelySelector(name, true, 3000, zkClient1, leaderPath, listener);

                    try
                    {
                        count.Wait();
                    }
                    catch (ThreadInterruptedException e1)
                    {
                    }
                    selector1.Start();

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

            int size = TestUtil.WaitUntil(5, () => { return msgList.Count; }, new TimeSpan(0, 0, 100));
            Assert.True(size == 5);
        }

    }
}
