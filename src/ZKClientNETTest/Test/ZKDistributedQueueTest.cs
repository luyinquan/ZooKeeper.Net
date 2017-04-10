using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using ZKClientNET.Queue;
using ZKClientNETTest.Util;
using ZKClientNET.Util;
using ZKClientNET.Client;

namespace ZKClientNETTest.Test
{
    public class ZKDistributedQueueTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKDistributedQueueTest));
        private ZKClient _zkClient;

        [OneTimeSetUp]
        public virtual void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port), new TimeSpan(0, 0, 0, 0, 5000));
            TestUtil.ReSetPathCreate(_zkClient, "/queue");
        }

        [OneTimeTearDown]
        public virtual void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        [Timeout(15000)]
        public void TestDistributedQueue()
        {       
            ZKDistributedQueue<long> distributedQueue = new ZKDistributedQueue<long>(_zkClient, "/queue");
            distributedQueue.Offer(17);
            distributedQueue.Offer(18);
            distributedQueue.Offer(19);

            Assert.True((17 == distributedQueue.Poll()));
            Assert.True((18 == distributedQueue.Poll()));
            Assert.True((19 == distributedQueue.Poll()));
            Assert.Zero(distributedQueue.Poll());
        }

        [Test]
        [Timeout(15000)]
        public void TestPeek()
        {
            ZKDistributedQueue<long> distributedQueue = new ZKDistributedQueue<long>(_zkClient, "/queue");
            distributedQueue.Offer(17);
            distributedQueue.Offer(18);

            Assert.True((17 == distributedQueue.Peek()));
            Assert.True((17 == distributedQueue.Peek()));
            Assert.True((17 == distributedQueue.Poll()));
            Assert.True((18 == distributedQueue.Peek()));
            Assert.True((18 == distributedQueue.Poll()));
            Assert.Zero(distributedQueue.Peek());
        }

        [Test]
        [Timeout(30000)]
        public void TestMultipleReadingThreads()
        {
            ZKDistributedQueue<long?> distributedQueue = new ZKDistributedQueue<long?>(_zkClient, "/queue");

            // insert 100 elements
            for (int i = 0; i < 100; i++)
            {
                distributedQueue.Offer(i);
            }
            // 3 reading threads
            ConcurrentHashSet<long?> readElements = new ConcurrentHashSet<long?>();
            List<Thread> threads = new List<Thread>();
            List<Exception> exceptions = new List<Exception>();
            for (int i = 0; i < 3; i++)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        while (true)
                        {
                            long? value = distributedQueue.Poll();
                            if (!value.HasValue)
                            {
                                return;
                            }
                            readElements.Add(value);
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                });
                threads.Add(thread);
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            Assert.True((0 == exceptions.Count));
            Assert.True((100 == readElements.Count));
        }

    }
}
