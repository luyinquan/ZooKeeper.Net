using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZkClientNET.Util;
using ZooKeeperNet;

namespace ZkClientNET.Test.ZkClientTest
{
   public  class ZkConnectionTest: AbstractConnectionTest
    {
        public ZkConnectionTest() : base(EstablishConnection())
        {
        }

        private static ZkConnection EstablishConnection()
        {
            ZkConnection zkConnection = new ZkConnection(string.Format("{0}:{1}", ip, port));
            new ZkClient(zkConnection);// connect
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
            string sequentialPath = _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential);
            int firstSequential = int.Parse(sequentialPath.Substring(2));
            Assert.True("/a" + ZkPathUtil.LeadingZeros(firstSequential++, 10) == sequentialPath);
            Assert.True("/a" + ZkPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/a" + ZkPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/b" + ZkPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/b", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/b" + ZkPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/b", new byte[0], CreateMode.EphemeralSequential));
            Assert.True("/a" + ZkPathUtil.LeadingZeros(firstSequential++, 10) == _connection.Create("/a", new byte[0], CreateMode.EphemeralSequential));
        }

    }
}
