using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;
using NUnit.Framework;
using ZKClientNET.Connection;
using ZKClientNET.Listener;
using ZKClientNETTest.Util;
using ZKClientNET.Client;

namespace ZKClientNETTest.Test
{
    public class ZKStateChangeTest
    {
        private StateOnlyConnection zkConn;
        private ZKClient client;
        private TestStateListener listener;

        [OneTimeSetUp]
        public void SetUp()
        {
            zkConn = new StateOnlyConnection();
            client = new ZKClient(zkConn);
            listener = new TestStateListener();
            client.SubscribeStateChanges(listener);
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            client.Close();
        }

        [Test]
        public void TestNewSessionEvent()
        {
            zkConn.ExpireSession();
            AssertTimed(1, () => { return listener.expiredEvents; });

            AssertTimed(0, () => { return listener.sessionEstablishErrors; });

            AssertTimed(1, () => { return listener.newSessionEvent; });
        }

        [Test]
        public void TestFailConnectEvent()
        {
            zkConn.failOnConnect = true;
            zkConn.ExpireSession();
            AssertTimed(1, () => { return listener.expiredEvents; });

            AssertTimed(1, () => { return listener.sessionEstablishErrors; });

            AssertTimed(0, () => { return listener.newSessionEvent; });

            client.Close();
        }

        private void AssertTimed(int expectedVal, Func<int> condition)
        {
            Assert.True(expectedVal == TestUtil.WaitUntil(expectedVal, condition, new TimeSpan(0, 0, 0, 5)));
        }

    }


    public class StateOnlyConnection : IZKConnection
    {
        public IWatcher _watcher { set; get; }
        public bool failOnConnect { set; get; } = false;

        public void Connect(IWatcher watcher)
        {
            _watcher = watcher;
            if (failOnConnect)
            {
                // As as example:
                throw new Exception("Testing connection failure");
            }

            Task.Factory.StartNew(() =>
            {
                _watcher.Process(new WatchedEvent(KeeperState.SyncConnected, EventType.None, null));
            });
        }

        public void ExpireSession()
        {
            _watcher.Process(new WatchedEvent(KeeperState.Expired, EventType.None, null));
        }

        public void Close()
        {
        }

        public string Create(string path, byte[] data, CreateMode mode)
        {
            throw new NotImplementedException();
        }

        public void AddAuthInfo(string scheme, byte[] auth)
        {
            throw new NotImplementedException();
        }

        public string Create(string path, byte[] data, List<ACL> acl, CreateMode mode)
        {
            throw new NotImplementedException();
        }

        public void Delete(string path)
        {
            throw new NotImplementedException();
        }

        public void Delete(string path, int version)
        {
            throw new NotImplementedException();
        }

        public bool Exists(string path, bool watch)
        {
            throw new NotImplementedException();
        }

        public KeyValuePair<List<ACL>, Stat> GetACL(string path)
        {
            throw new NotImplementedException();
        }

        public List<string> GetChildren(string path, bool watch)
        {
            throw new NotImplementedException();
        }

        public long GetCreateTime(string path)
        {
            throw new NotImplementedException();
        }

        public string GetServers()
        {
            return "test";
        }

        public ZooKeeper.States GetZookeeperState()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadData(string path, Stat stat, bool watch)
        {
            throw new NotImplementedException();
        }

        public void SetACL(string path, List<ACL> acl, int version)
        {
            throw new NotImplementedException();
        }

        public void WriteData(string path, byte[] data, int expectedVersion)
        {
            throw new NotImplementedException();
        }

        public Stat WriteDataReturnStat(string path, byte[] data, int expectedVersion)
        {
            throw new NotImplementedException();
        }

        public void ReConnect(IWatcher watcher)
        {
            throw new NotImplementedException();
        }
    }

    public class TestStateListener : IZKStateListener
    {
        public int expiredEvents = 0;
        public int newSessionEvent = 0;
        public int sessionEstablishErrors = 0;

        public void HandleNewSession()
        {
            newSessionEvent++;
        }

        public void HandleSessionEstablishmentError(Exception error)
        {
            sessionEstablishErrors++;
        }

        public void HandleStateChanged(KeeperState state)
        {
            if (state == KeeperState.Expired)
            {
                expiredEvents++;
            }
        }
    }

}
