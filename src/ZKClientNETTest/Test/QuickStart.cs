using NUnit.Framework;
using Org.Apache.Zookeeper.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Listener;
using ZKClientNET.Serialize;
using ZKClientNETTest.Util;
using ZooKeeperNet;

namespace ZKClientNETTest.Test
{
    public class QuickStart
    {

        /// <summary>
        /// 创建会话
        /// </summary>
        [Test]
        public void CreateSession()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                             .SessionTimeout(10000)
                             .ConnectionTimeout(10000)
                             .Serializer(new SerializableSerializer())
                             .Build();
            Console.WriteLine("conneted ok!");
            zkClient.Close();
            zkClient = null;
        }

        /// <summary>
        /// 创建节点
        /// </summary>
        [Test]
        public void CreateNode()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                             .SessionTimeout(10000)
                             .ConnectionTimeout(10000)
                             .Serializer(new SerializableSerializer())
                             .Build();
            Console.WriteLine("conneted ok!");
            User user = new User();
            user.Id = 1;
            user.Name = "testUser";

            string path = zkClient.Create("/testUserNode", user, CreateMode.Persistent);
            //输出创建节点的路径  
            Console.WriteLine("created path:" + path);
            zkClient.Close();
            zkClient = null;
        }

        /// <summary>
        /// 获取节点中的数据
        /// </summary>
        [Test]
        public void GetData()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                             .SessionTimeout(10000)
                             .ConnectionTimeout(10000)
                             .Serializer(new SerializableSerializer())
                             .Build();
            Console.WriteLine("conneted ok!");

            Stat stat = new Stat();
            //获取 节点中的对象  
            User user = zkClient.ReadData<User>("/testUserNode", stat);
            Console.WriteLine(user.Name);
            Console.WriteLine(stat);
            zkClient.Close();
            zkClient = null;
        }

        /// <summary>
        /// 判断节点是否存在
        /// </summary>
        [Test]
        public void Exists()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                             .SessionTimeout(10000)
                             .ConnectionTimeout(10000)
                             .Serializer(new SerializableSerializer())
                             .Build();
            Console.WriteLine("conneted ok!");

            bool e = zkClient.Exists("/testUserNode");
            //返回 true表示节点存在 ，false表示不存在  
            Console.WriteLine(e);
            zkClient.Close();
            zkClient = null;
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        [Test]
        public void Delete()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                             .SessionTimeout(10000)
                             .ConnectionTimeout(10000)
                             .Serializer(new SerializableSerializer())
                             .Build();
            Console.WriteLine("conneted ok!");

            //删除单独一个节点，返回true表示成功  
            bool e1 = zkClient.Delete("/testUserNode");
            //删除含有子节点的节点  
            bool e2 = zkClient.DeleteRecursive("/test");

            //返回 true表示节点成功 ，false表示删除失败  
            Console.WriteLine(e1);
            Console.WriteLine(e2);
            zkClient.Close();
            zkClient = null;
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        [Test]
        public void WriteData()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                              .SessionTimeout(10000)
                              .ConnectionTimeout(10000)
                              .Serializer(new SerializableSerializer())
                              .Build();
            Console.WriteLine("conneted ok!");

            User user = new User();
            user.Id = 2;
            user.Name = "testUser2";

            //testUserNode 节点的路径 
            // user 传入的数据对象
            zkClient.WriteData("/testUserNode", user);
            zkClient.Close();
            zkClient = null;
        }

        /// <summary>
        /// 订阅节点的信息改变（创建节点，删除节点，添加子节点）
        /// </summary>
        [Test]
        public void SubscribeChildChanges()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                             .SessionTimeout(10000)
                             .ConnectionTimeout(10000)
                             .Serializer(new SerializableSerializer())
                             .Build();
            Console.WriteLine("conneted ok!");
            ZKChildListener childListener = new ZKChildListener().ChildChange((parentPath, currentChilds) =>
            {
                Console.WriteLine(parentPath);
                Console.WriteLine(string.Join(".", currentChilds));
            });

            //"/testUserNode" 监听的节点，可以是现在存在的也可以是不存在的 
            zkClient.SubscribeChildChanges("/testUserNode3", childListener);
            Thread.Sleep(int.MaxValue);
            zkClient.Close();
            zkClient = null;
        }

        /// <summary>
        /// 订阅节点的数据内容的变化
        /// </summary>
        [Test]
        public void SubscribeDataChanges()
        {
            ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers)
                             .SessionTimeout(10000)
                             .ConnectionTimeout(10000)
                             .Serializer(new SerializableSerializer())
                             .Build();
            Console.WriteLine("conneted ok!");
            IZKDataListener dataListener = new ZKDataListener()
                .DataCreatedOrChange((dataPath, data) =>
                {
                    Console.WriteLine(dataPath + ":" + Convert.ToString(data));
                })
               .DataDeleted((dataPath) =>
                {
                    Console.WriteLine(dataPath);
                });
            zkClient.SubscribeDataChanges("/testUserNode", dataListener);
            Thread.Sleep(int.MaxValue);
            zkClient.Close();
            zkClient = null;
        }

    }

    [Serializable]
    public class User
    {
        public int Id { set; get; }

        public string Name { set; get; }
    }
}


