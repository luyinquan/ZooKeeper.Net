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
    public class ZKDistributedLockTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKDistributedLockTest));
        private ZKClient _zkClient;
        private string lockPath = "/zk/lock";

        [OneTimeSetUp]
        public virtual void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port), new TimeSpan(0, 0, 0, 0, 5000));
            TestUtil.ReSetPathCreate(_zkClient, lockPath);
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
        public void TestDistributedLock()
        {
            _zkClient.CreateRecursive(lockPath, null, CreateMode.Persistent);
            int integer = 0;
            List<string> msgList = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                Task.Factory.StartNew(() =>
                {
                    try
                    {
                        ZKDistributedLock _lock = ZKDistributedLock.NewInstance(_zkClient, lockPath);
                        _lock.Lock(0);
                        Interlocked.Increment(ref integer);
                        msgList.Add("thread " + integer);
                        Console.WriteLine("Thread " + integer + " got lock........");
                        Console.WriteLine(_lock.GetParticipantNodes());
                        if (integer == 3)
                        {
                            Thread.Sleep(1000);
                        }
                        if (integer == 5)
                        {
                            Thread.Sleep(700);
                        }
                        if (integer == 6 || integer == 11)
                        {
                            Thread.Sleep(500);
                        }
                        if (integer == 10)
                        {
                            Thread.Sleep(500);
                        }
                        if (integer == 15)
                        {
                            Thread.Sleep(400);
                        }
                        _lock.UnLock();
                        Console.WriteLine("thread " + integer + " unlock........");
                    }
                    catch (ThreadInterruptedException)
                    {
                    }
                });
            }

            int size = TestUtil.WaitUntil(20, () => { return msgList.Count; }, new TimeSpan(0, 0, 200));
            Assert.True(size == 20);

            bool flag = true;
            for (int i = 0; i < 20; i++)
            {
                if (msgList[i] != ("thread " + (i + 1)))
                {
                    flag = false;
                }
            }
            Assert.True(flag);

        }

    }
}
