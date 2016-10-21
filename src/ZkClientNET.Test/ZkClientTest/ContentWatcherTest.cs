using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZkClientNET.Test.ZkClientTest
{
    public class ContentWatcherTest
    {
        private static string FILE_NAME = "/ContentWatcherTest";
        protected ZkClient _zkClient;
        protected static readonly ILog LOG = LogManager.GetLogger(typeof(ContentWatcherTest));
        protected static string ip = ConfigurationManager.AppSettings["ip"];
        protected static string port = ConfigurationManager.AppSettings["port"];

        [OneTimeSetUp]
        public void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZkClient(string.Format("{0}:{1}", ip, port), new TimeSpan(0, 0, 0, 0, 5000));
            TestUtil.ReSetPathUnCreate(_zkClient, FILE_NAME);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        public void TestGetContent()
        {
            LOG.Info("--- testGetContent");
            ContentWatcher<string> watcher = new ContentWatcher<string>(_zkClient, FILE_NAME);
            watcher.Start();
            _zkClient.CreatePersistent(FILE_NAME, "a");
            Assert.True("a" == watcher.GetContent());

            // update the content
            _zkClient.WriteData(FILE_NAME, "b");

            string contentFromWatcher = TestUtil.WaitUntil("b", () => { return watcher.GetContent(); }, new TimeSpan(0, 0, 0, 5));

            Assert.True("b" == contentFromWatcher);
            watcher.Stop();
        }

        [Test]
        public void TestGetContentWaitTillCreated()
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
                catch (Exception e)
                {
                }
            });
            task.Start();

            // create content after 200ms
            task.Wait(200);
            _zkClient.CreatePersistent(FILE_NAME, "aaa");

            // we give the thread some time to pick up the change
            task.Wait(1000);
            Assert.True("aaa" == contentHolder.value);
        }

        [Test]
        public void TestHandlingNullContent()
        {
            LOG.Info("--- testHandlingNullContent");
            ContentWatcher<string> watcher = new ContentWatcher<string>(_zkClient, FILE_NAME);
            watcher.Start();
            _zkClient.CreatePersistent(FILE_NAME, null);
            Assert.True(string.IsNullOrEmpty(watcher.GetContent()));
            watcher.Stop();
        }
    }
}
