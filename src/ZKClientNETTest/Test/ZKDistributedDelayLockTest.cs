using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Lock;
using ZKClientNETTest.Util;
using ZooKeeperNet;

namespace ZKClientNETTest.Test
{
    public class ZKDistributedDelayLockTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKDistributedDelayLockTest));
        private ZKClient _zkClient;
        private string lockPath = "/zk/delylock1";
        private AutoResetEvent autoReset = new AutoResetEvent(false);

        [OneTimeSetUp]
        public virtual void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port), new TimeSpan(0, 0, 0, 0, 5000));
            TestUtil.ReSetPathUnCreate(_zkClient, lockPath);
        }

        [OneTimeTearDown]
        public virtual void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        public void TestDistributedDelayLock()
        {
            _zkClient.CreateRecursive(lockPath, null, CreateMode.Persistent);
            int index = 0;
            List<string> msgList = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                Task.Factory.StartNew(() =>
                {
                    ZKClient zkClient1 = ZKClientBuilder.NewZKClient()
                              .Servers(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port))
                              .SessionTimeout(10000)
                              .Build();
                    ZKDistributedDelayLock _lock = ZKDistributedDelayLock.NewInstance(zkClient1, lockPath);
                    autoReset.WaitOne();

                    _lock.Lock();
                    Console.WriteLine(Thread.CurrentThread.Name + ":lock....");
                    msgList.Add(Thread.CurrentThread.Name + ":unlock");
                    try
                    {
                        Thread.Sleep(1000);
                    }
                    catch (ThreadInterruptedException e)
                    {
                    }
                    Console.WriteLine(Thread.CurrentThread.Name + ":unlock....");
                    _lock.UnLock();
                });
                Interlocked.Increment(ref index);
            }

            autoReset.Set();

            int size = TestUtil.WaitUntil(5, () => { return msgList.Count; }, new TimeSpan(0, 0, 200));
            Assert.True(size == 5);
        }
    }
}
