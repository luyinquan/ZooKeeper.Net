using log4net;
using NUnit.Framework;
using org.apache.zookeeper;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZookeeperClient.Client;
using ZookeeperClient.Lock;
using ZookeeperClient.Util;


namespace ZookeeperClient.Test
{
    public class ZKDistributedLockTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(ZKDistributedLockTest));
        private string lockPath = "/zk/lock";
        private ZKClient _zkClient;

        /// <summary>
        /// 测试分布式锁
        /// </summary>
        [Test]
        public async Task TestDistributedLock()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(TestUtil.zkServers);
            await TestUtil.ReSetPathUnCreate(_zkClient, lockPath);
            await  _zkClient.CreateRecursiveAsync(lockPath, null, CreateMode.PERSISTENT);
            var integer = 0;
            var msgList = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                await Task.Run(async () =>
                 {
                     try
                     {
                         var _lock = new ZKDistributedLock(_zkClient, lockPath);
                         await _lock.LockAsync();
                         Interlocked.Increment(ref integer);
                         msgList.Add("thread " + integer);
                         Console.WriteLine("Thread " + integer + " got lock........");
                         Console.WriteLine(string.Join(".", _lock.GetParticipantNodes()));
                         if (integer == 3)
                         {
                             await Task.Delay(1000);
                         }
                         if (integer == 5)
                         {
                             await Task.Delay(700);
                         }
                         if (integer == 6 || integer == 11)
                         {
                             await Task.Delay(500);
                         }
                         if (integer == 10)
                         {
                             await Task.Delay(500);
                         }
                         if (integer == 15)
                         {
                             await Task.Delay(400);
                         }
                         await _lock.UnLockAsync();
                         Console.WriteLine("thread " + integer + " unlock........");
                     }
                     catch (ThreadInterruptedException)
                     {
                     }
                 });
            }

            int size = TestUtil.WaitUntil(20, () => { return msgList.Count; }, TimeSpan.FromSeconds(200));
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

            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();

        }
    }
}
