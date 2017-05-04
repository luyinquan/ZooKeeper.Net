using log4net;
using System;
using System.Collections.Generic;
using System.Threading;
using ZooKeeperClient.Queue;
using ZooKeeperClient.Util;
using ZooKeeperClient.Client;
using System.Threading.Tasks;
using org.apache.zookeeper;
using Xunit;

namespace ZooKeeperClient.Test
{
    public class ZKDistributedQueueTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKDistributedQueueTest));
        private string queuePath = "/zk/queue";
        private ZKClient _zkClient;

        [Fact]
        public async Task TestDistributedQueue()
        {
            using (_zkClient = new ZKClient(TestUtil.zkServers))
            {
                await TestUtil.ReSetPathUnCreate(_zkClient, queuePath);
                await _zkClient.CreateRecursiveAsync(queuePath, null, CreateMode.PERSISTENT);

                var queue = new ZKDistributedQueue<long>(_zkClient, queuePath);
                await queue.OfferAsync(17);
                await queue.OfferAsync(18);
                await queue.OfferAsync(19);
                Assert.True((17 == await queue.PollAsync()));
                Assert.True((18 == await queue.PollAsync()));
                Assert.True((19 == await queue.PollAsync()));
                Assert.True(0 == await queue.PollAsync());
            }
        }

        [Fact]
        public async Task TestPeek()
        {
            using (_zkClient = new ZKClient(TestUtil.zkServers))
            {
                await TestUtil.ReSetPathUnCreate(_zkClient, queuePath);
                await _zkClient.CreateRecursiveAsync(queuePath, null, CreateMode.PERSISTENT);

                var queue = new ZKDistributedQueue<long>(_zkClient, queuePath);;
                await queue.OfferAsync(17);
                await queue.OfferAsync(18);

                Assert.True((17 == await queue.PeekAsync()));
                Assert.True((17 == await queue.PeekAsync()));
                Assert.True((17 == await queue.PollAsync()));
                Assert.True((18 == await queue.PeekAsync()));
                Assert.True((18 == await queue.PollAsync()));
                Assert.True(0 == await queue.PollAsync());
            }
        }

        [Fact]
        public async Task TestMultipleReadingThreads()
        {
            using (_zkClient = new ZKClient(TestUtil.zkServers))
            {
                await TestUtil.ReSetPathUnCreate(_zkClient, queuePath);
                await _zkClient.CreateRecursiveAsync(queuePath, null, CreateMode.PERSISTENT);

                var queue = new ZKDistributedQueue<long>(_zkClient, queuePath);

                // insert 100 elements
                for (int i = 0; i < 100; i++)
                {
                    await queue.OfferAsync(i);
                }
                // 3 reading threads
                ConcurrentHashSet<long?> readElements = new ConcurrentHashSet<long?>();
                var tasks = new Task[3];
                List<Exception> exceptions = new List<Exception>();
                for (int i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(async () =>
                     {
                         try
                         {
                             while (true)
                             {
                                 long? value = await queue.PollAsync();
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
                }

                Task.WaitAll(tasks);

                Assert.True((0 == exceptions.Count));
                Assert.True((100 == readElements.Count));
            }

        }
    }
}
