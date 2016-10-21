using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkClientNET.Util;

namespace ZkClientNET.Test.ZkClientTest
{
    public class DistributedQueueTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(DistributedQueueTest));
        private ZkClient _zkClient;
        protected static string ip = ConfigurationManager.AppSettings["ip"];
        protected static string port = ConfigurationManager.AppSettings["port"];

        [OneTimeSetUp]
        public virtual void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZkClient(string.Format("{0}:{1}", ip, port), new TimeSpan(0, 0, 0, 0, 5000));
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
            DistributedQueue<long> distributedQueue = new DistributedQueue<long>(_zkClient, "/queue");
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
            DistributedQueue<long> distributedQueue = new DistributedQueue<long>(_zkClient, "/queue");
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
            DistributedQueue<long?> distributedQueue = new DistributedQueue<long?>(_zkClient, "/queue");

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
