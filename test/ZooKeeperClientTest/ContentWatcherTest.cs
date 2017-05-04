using log4net;
using System;
using System.Threading.Tasks;
using Xunit;
using ZooKeeperClient.Client;
using ZooKeeperClient.Listener;
using ZooKeeperClient.Util;

namespace ZooKeeperClient.Test
{
    public class ContentWatcherTest : IDisposable
    {
        private static string FILE_NAME = "/ContentWatcherTest";
        protected ZKClient _zkClient;
        protected static readonly ILog LOG = LogManager.GetLogger(typeof(ContentWatcherTest));


        public ContentWatcherTest()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(TestUtil.zkServers);
            TestUtil.ReSetPathUnCreate(_zkClient, FILE_NAME).GetAwaiter().GetResult();
        }

        public void Dispose()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }


        [Fact]
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

        [Fact]
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

        [Fact]
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
