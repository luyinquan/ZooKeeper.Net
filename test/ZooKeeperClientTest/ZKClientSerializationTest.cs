using System;
using System.Threading.Tasks;
using Xunit;
using ZooKeeperClient.Client;
using ZooKeeperClient.Util;

namespace ZooKeeperClient.Test
{
    public class ZKClientSerializationTest
    {
        [Fact]
        public async Task TestBytes()
        {
            using (ZKClient _zkClient = new ZKClient(TestUtil.zkServers))
            {
                await TestUtil.ReSetPathUnCreate(_zkClient, "/a");
                byte[] bytes = new byte[100];
                new Random().NextBytes(bytes);
                await _zkClient.CreatePersistentAsync("/a", bytes);
                byte[] readBytes = await _zkClient.GetDataAsync<byte[]>("/a");
            }
        }

        [Fact]
        public async Task TestSerializables()
        {
            using (ZKClient _zkClient = new ZKClient(TestUtil.zkServers))
            {
                await TestUtil.ReSetPathUnCreate(_zkClient, "/a");
                string data = "hello world";
                await _zkClient.CreatePersistentAsync("/a", data);
                string readData = await _zkClient.GetDataAsync<string>("/a");
            }
        }
    }
}
