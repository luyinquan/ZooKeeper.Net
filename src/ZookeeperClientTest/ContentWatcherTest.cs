using log4net;
using NUnit.Framework;
using System;
using System.Threading.Tasks;
using ZookeeperClient.Client;
using ZookeeperClient.Listener;
using ZookeeperClient.Util;

namespace ZookeeperClient.Test
{
    public class ContentWatcherTest
    {
        private static string FILE_NAME = "/ContentWatcherTest";
        protected ZKClient _zkClient;
        protected static readonly ILog LOG = LogManager.GetLogger(typeof(ContentWatcherTest));


        [OneTimeSetUp]
        public async Task SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(TestUtil.zkServers);
            await TestUtil.ReSetPathUnCreate(_zkClient, FILE_NAME);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        public async Task TestGetContent()
        {
            LOG.Info("--- testGetContent");
            ContentWatcher<string> watcher = new ContentWatcher<string>(_zkClient, FILE_NAME);
            watcher.Start();
            await _zkClient.CreatePersistentAsync(FILE_NAME, "a");
            Assert.True("a" == watcher.GetContent());

            // update the content
            await _zkClient.SetDataAsync(FILE_NAME, "b");

            string contentFromWatcher = TestUtil.WaitUntil(
                "b",
                () => { return watcher.GetContent(); },
                TimeSpan.FromSeconds(5));

            Assert.True("b" == contentFromWatcher);
            watcher.Stop();
        }

        [Test]
        public async Task TestGetContentWaitTillCreated()
        {
            LOG.Info("--- testGetContentWaitTillCreated");
            Holder<string> contentHolder = new Holder<string>();

            Task task = new Task(() =>
            {
                ContentWatcher<string> watcher = new ContentWatcher<string>(_zkClient, FILE_NAME);
                try
                {
                    watcher.Start();
                    contentHolder.value = watcher.GetContent();
                    watcher.Stop();
                }
                catch (Exception)
                {
                }
            });
            task.Start();

            // create content after 200ms
            await Task.Delay(200);
            await _zkClient.CreatePersistentAsync(FILE_NAME, "aaa");

            // we give the thread some time to pick up the change
            await Task.Delay(1000);
            Assert.True("aaa" == contentHolder.value);
        }

        [Test]
        public async Task TestHandlingNullContent()
        {
            LOG.Info("--- testHandlingNullContent");
            ContentWatcher<string> watcher = new ContentWatcher<string>(_zkClient, FILE_NAME);
            watcher.Start();
            await _zkClient.CreatePersistentAsync(FILE_NAME, null);
            Assert.True(string.IsNullOrEmpty(watcher.GetContent()));
            watcher.Stop();
        }
    }
}
