using NUnit.Framework;
using ZookeeperClient.Connection;
using ZookeeperClient.Util;
using org.apache.zookeeper;
using System.Threading.Tasks;
using System;

namespace ZookeeperClient.Test
{
    public class TestWatcher : Watcher
    {
        public override Task process(WatchedEvent @event)
        {
            return Task.CompletedTask;
        }
    }

    public class ZKConnectionTest
    {
        protected IZKConnection _connection;

        private static ZKConnection EstablishConnection()
        {
            ZKConnection zkConnection = new ZKConnection(TestUtil.zkServers);
            zkConnection.Connect(new TestWatcher());
            return zkConnection;
        }


        [Test]
        public async Task TestSequentials()
        {
            _connection = EstablishConnection();

            string sequentialPath = await _connection.CreateAsync("/a", new byte[0], CreateMode.EPHEMERAL_SEQUENTIAL);
            int firstSequential = int.Parse(sequentialPath.Substring(2));
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == sequentialPath);
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == 
                await _connection.CreateAsync("/a", new byte[0], CreateMode.EPHEMERAL_SEQUENTIAL));
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == 
                await _connection.CreateAsync("/a", new byte[0], CreateMode.EPHEMERAL_SEQUENTIAL));
            Assert.True("/b" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == 
                await _connection.CreateAsync("/b", new byte[0], CreateMode.EPHEMERAL_SEQUENTIAL));
            Assert.True("/b" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == 
                await _connection.CreateAsync("/b", new byte[0], CreateMode.EPHEMERAL_SEQUENTIAL));
            Assert.True("/a" + ZKPathUtil.LeadingZeros(firstSequential++, 10) == 
                await _connection.CreateAsync("/a", new byte[0], CreateMode.EPHEMERAL_SEQUENTIAL));
            _connection.Close();
            _connection = null;
        }

    }
}
