using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZkClientNET.Serialize;

namespace ZkClientNET.Test.ZkClientTest
{
    public class ZkClientSerializationTest
    {
        protected static string ip = ConfigurationManager.AppSettings["ip"];
        protected static string port = ConfigurationManager.AppSettings["port"];

        [Test]
        public void TestBytes()
        {
            ZkClient _zkClient = new ZkClient(string.Format("{0}:{1}", ip, port), new TimeSpan(0, 0, 0, 0, 2000), new TimeSpan(0, 0, 0, 0, 3000), new BytesPushThroughSerializer());
            TestUtil.ReSetPathUnCreate(_zkClient, "/a");
            byte[] bytes = new byte[100];
            new Random().NextBytes(bytes);
            _zkClient.CreatePersistent("/a", bytes);
            byte[] readBytes = _zkClient.ReadData<byte[]>("/a");
            _zkClient.Close();
            _zkClient = null;
        }

        [Test]
        public void TestSerializables()
        {
            ZkClient _zkClient = new ZkClient(string.Format("{0}:{1}", ip, port), new TimeSpan(0, 0, 0, 0, 2000), new TimeSpan(0, 0, 0, 0, 3000), new SerializableSerializer());
            TestUtil.ReSetPathUnCreate(_zkClient, "/a");
            string data = "hello world";
            _zkClient.CreatePersistent("/a", data);
            string readData = _zkClient.ReadData<string>("/a");
            _zkClient.Close();
            _zkClient = null;
        }
    }
}
