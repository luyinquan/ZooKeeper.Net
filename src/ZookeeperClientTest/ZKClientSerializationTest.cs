using NUnit.Framework;
using System;
using System.Threading.Tasks;
using ZookeeperClient.Client;
using ZookeeperClient.Util;

namespace ZookeeperClient.Test
{
    public class ZKClientSerializationTest
    {
        [Test]
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

        [Test]
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
