using NUnit.Framework;
using System.Collections.Generic;
using ZKClientNET.Connection;
using ZKClientNET.Util;
using ZooKeeperNet;
using ZKClientNETTest.Util;
using ZKClientNET.Client;

namespace ZKClientNETTest.Test
{
    public  class ZKConnectionTest
    {
        protected IZKConnection _connection;

        private static ZKConnection EstablishConnection()
        {
            ZKConnection zkConnection = new ZKConnection(string.Format("{0}:{1}", TestUtil.ip, TestUtil.port));
            new ZKClient(zkConnection);// connect
            return zkConnection;
        }

        [Test]
        public void TestGetChildren_OnEmptyFileSystem()
        {
            InMemoryConnection connection = new InMemoryConnection();
            List<string> children = connection.GetChildren("/", false);
            Assert.True(0 == children.Count);
            connection.Close();
            connection = null;
        }

        [Test]
        public void TestSequentials()
        {
            _connection = EstablishConnection();

            string sequentialPath = _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential);
            int firstSequential = int.Parse(sequentialPath.Substring(2));
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == sequentialPath);
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/b" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/b", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/b" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/b", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential));
            _connection.Close();
            _connection = null;
        }

    }
}
