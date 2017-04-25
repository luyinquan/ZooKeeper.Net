using NUnit.Framework;
using org.apache.zookeeper;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZookeeperClient.Client;
using ZookeeperClient.Listener;
using ZookeeperClient.Util;


namespace ZookeeperClient.Test
{
    public class QuickStart
    {
        /// <summary>
        /// 创建会话
        /// </summary>
        [Test]
        public void CreateSession()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {            
                Console.WriteLine("conneted ok!");
            }
        }

        /// <summary>
        /// 创建节点
        /// </summary>
        [Test]
        public async Task CreateNode()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                User user = new User();
                user.Id = 1;
                user.Name = "testUser";
                await zkClient.DeleteRecursiveAsync("/testUserNode");
                var path = await zkClient.CreateAsync("/testUserNode", user, CreateMode.PERSISTENT);
                //输出创建节点的路径  
                Console.WriteLine("created path:" + path);
            }
        }

        /// <summary>
        /// 获取节点中的数据
        /// </summary>
        [Test]
        public async Task GetData()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                //获取 节点中的对象  
                User user = await zkClient.GetDataAsync<User>("/testUserNode");
                Console.WriteLine(user.Name);
            }
        }

        /// <summary>
        /// 判断节点是否存在
        /// </summary>
        [Test]
        public async Task Exists()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                bool e = await zkClient.ExistsAsync("/testUserNode");
                //返回 true表示节点存在 ，false表示不存在  
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        [Test]
        public async Task Delete()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                //删除单独一个节点，返回true表示成功  
                bool e1 = await zkClient.DeleteAsync("/testUserNode");
                //删除含有子节点的节点  
                bool e2 = await zkClient.DeleteRecursiveAsync("/test");

                //返回 true表示节点成功 ，false表示删除失败  
                Console.WriteLine(e1);
                Console.WriteLine(e2);
            }
        }

        /// <summary>
        /// 更新数据
        /// </summary>
        [Test]
        public async Task SetData()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                User user = new User();
                user.Id = 2;
                user.Name = "testUser2";
                //testUserNode 节点的路径 
                //user 传入的数据对象
                await zkClient.SetDataAsync<User>("/testUserNode", user);
            }
        }

        /// <summary>
        /// 订阅节点的信息改变（创建节点，删除节点，添加子节点）
        /// </summary>
        [Test]
        public void SubscribeChildChanges()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                IZKChildListener childListener = new ZKChildListener();
                //子节点内容变化
                childListener.ChildChangeHandler = async (parentPath, currentChilds) =>
                 {
                     await Task.Run(() =>
                     {
                         Console.WriteLine(parentPath);
                         Console.WriteLine(string.Join(".", currentChilds));
                     });
                 };
                //子节点数量变化
                childListener.ChildCountChangedHandler = async (parentPath, currentChilds) =>
                {
                    await Task.Run(() =>
                    {
                        Console.WriteLine(parentPath);
                        Console.WriteLine(string.Join(".", currentChilds));
                    });
                };
                //"/testUserNode" 监听的节点，可以是现在存在的也可以是不存在的 
                zkClient.SubscribeChildChanges("/testUserNode3", childListener);
                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }

        /// <summary>
        /// 订阅节点的数据内容的变化
        /// </summary>
        [Test]
        public void SubscribeDataChanges()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                IZKDataListener dataListener = new ZKDataListener();
                // 节点创建和节点内容变化
                dataListener.DataCreatedOrChangeHandler = async (dataPath, data) =>
                      {
                          await Task.Run(() =>
                          {
                              Console.WriteLine(dataPath + ":" + Convert.ToString(data));
                          });
                      };
                // 节点删除
                dataListener.DataDeletedHandler = async (dataPath) =>
                      {
                          await Task.Run(() =>
                          {
                              Console.WriteLine(dataPath);
                          });

                      };
                // 节点创建
                dataListener.DataCreatedHandler = async (dataPath, data) =>
                {
                    await Task.Run(() =>
                    {
                        Console.WriteLine(dataPath + ":" + Convert.ToString(data));
                    });
                };
                // 节点内容变化
                dataListener.DataChangeHandler = async (dataPath, data) =>
                {
                    await Task.Run(() =>
                    {
                        Console.WriteLine(dataPath);
                    });

                };
                zkClient.SubscribeDataChanges("/testUserNode", dataListener);
                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }

        /// <summary>
        /// 客户端状态监听
        /// </summary>
        [Test]
        public void SubscribeStateChanges()
        {
            using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
            {
                IZKStateListener stateListener = new ZKStateListener();
                //状态改变
                stateListener.StateChangedHandler = async (state) =>
                {
                    await Task.Run(() =>
                    {
                        Console.WriteLine(state.ToString());
                    });
                };
                //会话失效
                stateListener.SessionExpiredHandler = async (path) =>
                {
                    await Task.Run(() =>
                    {
                        Console.WriteLine(path);
                    });
                };
               //创建会话
                stateListener.NewSessionHandler = async () =>
                {
                    await Task.Run(() =>{});
                };
               //会话失败
                stateListener.SessionEstablishmentErrorHandler = async (ex) =>
                {
                    await Task.Run(() =>
                    {
                        Console.WriteLine(ex.Message);
                    });

                };
                zkClient.SubscribeStateChanges(stateListener);
                Thread.Sleep(TimeSpan.FromSeconds(60));
            }
        }
    }

    [Serializable]
    public class User
    {
        public int Id { set; get; }

        public string Name { set; get; }
    }
}


