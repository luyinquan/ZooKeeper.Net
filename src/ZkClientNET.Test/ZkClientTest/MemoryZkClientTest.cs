using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkClientNET.Exceptions;
using ZkClientNET.Util;
using ZooKeeperNet;

namespace ZkClientNET.Test.ZkClientTest
{
    public class MemoryZkClientTest : AbstractBaseZkClientTest
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZkClient(new InMemoryConnection());
            TestUtil.ReSetPathUnCreate(_zkClient, "/a");
            TestUtil.ReSetPathUnCreate(_zkClient, "/a/a");
            TestUtil.ReSetPathUnCreate(_zkClient, "/a/a/a");
            TestUtil.ReSetPathUnCreate(_zkClient, "/gaga");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        public void TestGetChildren()
        {
            string path1 = "/a";
            string path2 = "/a/a";
            string path3 = "/a/a/a";

            _zkClient.Create(path1, null, CreateMode.Persistent);
            _zkClient.Create(path2, null, CreateMode.Persistent);
            _zkClient.Create(path3, null, CreateMode.Persistent);
            Assert.True(1 == _zkClient.GetChildren(path1).Count);
            Assert.True(1 == _zkClient.GetChildren(path2).Count);
            Assert.True(0 == _zkClient.GetChildren(path3).Count);
        }


        [Test]
        public void TestUnableToConnect()
        {
            LOG.Info("--- testUnableToConnect");
            // we are using port 4711 to avoid conflicts with the zk server that is
            // started by the Spring context
            new ZkClient(string.Format("{0}:{1}", ip, port), new TimeSpan(0, 0, 0, 0, 5000));
        }

        [Test]
        public void TestWriteAndRead()
        {
            LOG.Info("--- testWriteAndRead");
            string data = "something";
            string path = "/a";
            _zkClient.CreatePersistent(path, data);
            string data2 = _zkClient.ReadData<string>(path);
            Assert.True(data == data2);
            _zkClient.Delete(path);
        } 

        [Test]
        public void TestDelete()
        {
            LOG.Info("--- testDelete");
            string path = "/a";
            Assert.False(_zkClient.Delete(path));
            _zkClient.CreatePersistent(path, null);
            Assert.True(_zkClient.Delete(path));
            Assert.False(_zkClient.Delete(path));
        }

        [Test]
        public void TestDeleteWithVersion()
        {
            LOG.Info("--- testDelete");
            string path = "/a";
            Assert.False(_zkClient.Delete(path, 0));
            _zkClient.CreatePersistent(path, null);
            Assert.True(_zkClient.Delete(path, 0));
            _zkClient.CreatePersistent(path, null);
            _zkClient.WriteData(path, new byte[0]);
            Assert.True(_zkClient.Delete(path, 1));
            _zkClient.CreatePersistent(path, null);
            try
            {
                _zkClient.Delete(path, 1);
                Assert.Fail("Bad version excpetion expected.");
            }
            catch (ZkBadVersionException e)
            {
                // expected
            }
            Assert.True(_zkClient.Delete(path, 0));
        }

        [Test]
        public void TestDeleteRecursive()
        {
            LOG.Info("--- testDeleteRecursive");

            // should be able to call this on a not existing directory
            _zkClient.DeleteRecursive("/doesNotExist");
        }

        [Test]
        public void TestWaitUntilExists()
        {
            LOG.Info("--- testWaitUntilExists");

            // create /gaga node asynchronously
            Task.Factory.StartNew(() =>
            {
                try
                {
                    Thread.Sleep(100);
                    _zkClient.CreatePersistent("/gaga");
                }
                catch (Exception e)
                {
                    // ignore
                }

            });

            // wait until this was created
            Assert.True(_zkClient.WaitUntilExists("/gaga", new TimeSpan(0, 0, 0, 5)));
            Assert.True(_zkClient.Exists("/gaga"));

            // waiting for /neverCreated should timeout
            Assert.False(_zkClient.WaitUntilExists("/neverCreated", new TimeSpan(0, 0, 0, 0, 100)));
        }

        [Test]
        public void TestDataChanges1()
        {
            LOG.Info("--- testDataChanges1");
            string path = "/a";
            Holder<string> holder = new Holder<string>();

            IZkDataListener listener = new ZkDataListener1(holder);
            _zkClient.SubscribeDataChanges(path, listener);
            _zkClient.CreatePersistent(path, "aaa");

            // wait some time to make sure the event was triggered

            string contentFromHolder = TestUtil.WaitUntil("b", () => { return holder.value; }, new TimeSpan(0, 0, 0, 5));

            Assert.True("aaa" == contentFromHolder);
        }

        private class ZkDataListener1 : IZkDataListener
        {
            private Holder<string> holder;

            public ZkDataListener1(Holder<string> holder)
            {
                this.holder = holder;
            }

            public void HandleDataChange(string dataPath, object data)
            {
                holder.value = (string)data;
            }

            public void HandleDataDeleted(string dataPath)
            {
                holder.value = null;
            }
        }

        [Test]
        public void TestDataChanges2()
        {
            LOG.Info("--- testDataChanges2");
            string path = "/a";
            int countChanged = 0;
            int countDeleted = 0;

            var _listener= new ZkDataListener2(countChanged, countDeleted);
            IZkDataListener listener = _listener;

            _zkClient.SubscribeDataChanges(path, listener);

            // create node
            _zkClient.CreatePersistent(path, "aaa");

            // wait some time to make sure the event was triggered
            TestUtil.WaitUntil(1, () => { return _listener.countChanged; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(1 == _listener.countChanged);
            Assert.True(0 == _listener.countDeleted);


            _listener.countChanged = 0;
            _listener.countDeleted = 0;

            // delete node, this should trigger a delete event
            _zkClient.Delete(path);
            // wait some time to make sure the event was triggered
            TestUtil.WaitUntil(1, () => { return _listener.countDeleted; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(0 == _listener.countChanged);
            Assert.True(1 == _listener.countDeleted);

            // test if watch was reinstalled after the file got deleted
            _listener.countChanged = 0;
            _zkClient.CreatePersistent(path, "aaa");

            // wait some time to make sure the event was triggered
            TestUtil.WaitUntil(1, () => { return _listener.countChanged; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(1 == _listener.countChanged);

            // test if changing the contents notifies the listener
            _zkClient.WriteData(path, "bbb");

            // wait some time to make sure the event was triggered
            TestUtil.WaitUntil(2, () => { return _listener.countChanged; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(2 == _listener.countChanged);
        }

        private class ZkDataListener2 : IZkDataListener
        {
            public int countChanged = 0;
            public int countDeleted = 0;

            public ZkDataListener2(int countChanged, int countDeleted)
            {
                this.countChanged = countChanged;
                this.countDeleted = countDeleted;
            }

            public void HandleDataChange(string dataPath, object data)
            {
                Interlocked.Increment(ref countChanged);
            }

            public void HandleDataDeleted(string dataPath)
            {
                Interlocked.Increment(ref countDeleted);
            }
        }

        [Test]
        public void TestHandleChildChanges()
        {
            LOG.Info("--- testHandleChildChanges");
            string path = "/a";
            int count = 0;
            Holder<List<string>> children = new Holder<List<string>>();
            var _listener = new ZkChildListener(count, children);
            IZkChildListener listener = _listener;
            _zkClient.SubscribeChildChanges(path, listener);

            // ----
            // Create the root node should throw the first child change event
            // ----
            _zkClient.CreatePersistent(path);

            // wait some time to make sure the event was triggered
            TestUtil.WaitUntil(1, () => { return _listener.count; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(1 == _listener.count);
            Assert.True(0 == _listener.children.value.Count);

            // ----
            // Creating a child node should throw another event
            // ----
            _listener.count = 0;
            _zkClient.CreatePersistent(path + "/child1");

            // wait some time to make sure the event was triggered
            TestUtil.WaitUntil(1, () => { return _listener.count; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(1 == _listener.count);
            Assert.True(1 == _listener.children.value.Count);
            Assert.True("child1" == _listener.children.value[0]);

            // ----
            // Creating another child and deleting the node should also throw an event
            // ----
            _listener.count = 0;
            _zkClient.CreatePersistent(path + "/child2");
            _zkClient.DeleteRecursive(path);

            // wait some time to make sure the event was triggered
            bool eventReceived = TestUtil.WaitUntil(true, () => { return _listener.count > 0 && _listener.children.value == null; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(eventReceived);
            Assert.Null(_listener.children.value);

            // ----
            // Creating root again should throw an event
            // ----
            _listener.count = 0;
            _zkClient.CreatePersistent(path);

            // wait some time to make sure the event was triggered
            eventReceived = TestUtil.WaitUntil(true, () => { return _listener.count > 0 && _listener.children.value != null; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(eventReceived);
            Assert.True(0 == _listener.children.value.Count);

            // ----
            // Creating child now should throw an event
            // ----
            _listener.count = 0;
            _zkClient.CreatePersistent(path + "/child");

            // wait some time to make sure the event was triggered
            eventReceived = TestUtil.WaitUntil(true, () => { return _listener.count > 0; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(eventReceived);
            Assert.True(1 == _listener.children.value.Count);
            Assert.True("child" == _listener.children.value[0]);

            // ----
            // Deleting root node should throw an event
            // ----
            _listener.count = 0;
            _zkClient.DeleteRecursive(path);

            // wait some time to make sure the event was triggered
            eventReceived = TestUtil.WaitUntil(true, () => { return _listener.count > 0 && _listener.children.value == null; }, new TimeSpan(0, 0, 0, 5));
            Assert.True(eventReceived);
            Assert.Null(_listener.children.value);
        }

        private class ZkChildListener : IZkChildListener
        {
            public int count;

            public Holder<List<string>> children { set; get; }

            public ZkChildListener(int count, Holder<List<string>> children)
            {
                this.count = count;
                this.children = children;
            }

            public void HandleChildChange(string parentPath, List<string> currentChilds)
            {
                Interlocked.Increment(ref count);
                children.value = currentChilds;
            }
        }

        [Test]
        public void TestGetCreationTime()
        {
            long start = DateTime.Now.ToUnixTime();
            Thread.Sleep(100);
            string path = "/a";
            _zkClient.CreatePersistent(path);
            Thread.Sleep(100);
            long end = DateTime.Now.ToUnixTime();
            long creationTime = _zkClient.GetCreationTime(path);
            Assert.True(start < creationTime && end > creationTime);
        }

        [Test]
        public void TestNumberOfListeners()
        {
            var zkChildListener = new Mock<IZkChildListener>();
            _zkClient.SubscribeChildChanges("/", zkChildListener.Object);
            Assert.True(1 == _zkClient.NumberOfListeners());

            var zkDataListener = new Mock<IZkDataListener>();
            _zkClient.SubscribeDataChanges("/a", zkDataListener.Object);
            Assert.True(2 == _zkClient.NumberOfListeners());

            _zkClient.SubscribeDataChanges("/b", zkDataListener.Object);
            Assert.True(3 == _zkClient.NumberOfListeners());

            var zkStateListener = new Mock<IZkStateListener>();
            _zkClient.SubscribeStateChanges(zkStateListener.Object);
            Assert.True(4 == _zkClient.NumberOfListeners());

            _zkClient.UnSubscribeChildChanges("/", zkChildListener.Object);
            Assert.True(3 == _zkClient.NumberOfListeners());

            _zkClient.UnSubscribeDataChanges("/b", zkDataListener.Object);
            Assert.True(2 == _zkClient.NumberOfListeners());

            _zkClient.UnSubscribeDataChanges("/a", zkDataListener.Object);
            Assert.True(1 == _zkClient.NumberOfListeners());

            _zkClient.UnSubscribeStateChanges(zkStateListener.Object);
            Assert.True(0 == _zkClient.NumberOfListeners());
        }

    }
}
