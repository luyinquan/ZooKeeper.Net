## ZookeeperClient
A zookeeper client, that makes life a little easier. Implemented by .Net. Reference https://github.com/sgroschupf/zkclient https://github.com/yuluows/zkclient 

## 使用说明
    
### 创建会话
	   using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
       {
            Console.WriteLine("conneted ok!");
       }

### 创建节点
	   using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
       {
            var user = new User();
            user.Id = 1;
            user.Name = "testUser";
            await zkClient.DeleteRecursiveAsync("/testUserNode");
            var path = await zkClient.CreateAsync("/testUserNode", user, CreateMode.PERSISTENT);
            //输出创建节点的路径  
            Console.WriteLine("created path:" + path);
       }

### 获取节点中的数据
	 using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
     {
            //获取 节点中的对象  
            var user = await zkClient.GetDataAsync<User>("/testUserNode");
            Console.WriteLine(user.Name);
     }

### 判断节点是否存在
	 using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
     {
            var e = await zkClient.ExistsAsync("/testUserNode");
            //返回 true表示节点存在 ，false表示不存在  
            Console.WriteLine(e);
     }

### 删除节点
	using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
     {
            //删除单独一个节点，返回true表示成功  
            var e1 = await zkClient.DeleteAsync("/testUserNode");
            //删除含有子节点的节点  
            var e2 = await zkClient.DeleteRecursiveAsync("/test");

            //返回 true表示节点成功 ，false表示删除失败  
            Console.WriteLine(e1);
            Console.WriteLine(e2);
     }

### 更新数据
	 using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
     {
            var user = new User();
            user.Id = 2;
            user.Name = "testUser2";

            //testUserNode 节点的路径 
            //user 传入的数据对象
            await zkClient.SetDataAsync<User>("/testUserNode", user);
     }

### 订阅节点的信息改变（创建节点，删除节点，添加子节点）
	 using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
     {
                var childListener = new ZKChildListener();
                childListener.ChildChangeHandler = async (parentPath, currentChilds) =>
                 {
                     await Task.Run(() =>
                     {
                         Console.WriteLine(parentPath);
                         Console.WriteLine(string.Join(".", currentChilds));
                     });
                 };
                //"/testUserNode" 监听的节点，可以是现在存在的也可以是不存在的 
                zkClient.SubscribeChildChanges("/testUserNode3", childListener);
     }

### 订阅节点的数据内容的变化
	 using (ZKClient zkClient = ZKClientBuilder.NewZKClient(TestUtil.zkServers).Build())
     {
                var dataListener = new ZKDataListener();
                dataListener.DataCreatedOrChangeHandler = async (dataPath, data) =>
                  {
                      await Task.Run(() =>
                      {
                          Console.WriteLine(dataPath + ":" + Convert.ToString(data));
                      });
                  };
                dataListener.DataDeletedHandler = async (dataPath) =>
                  {
                      await Task.Run(() =>
                      {
                          Console.WriteLine(dataPath);
                      });
                  };
                zkClient.SubscribeDataChanges("/testUserNode", dataListener);
     }

    

    [Serializable]
    public class User
    {
        public int Id { set; Get; }

        public string Name { set; Get; }
    }
     
	 
## 扩展功能

### 分布式锁
     using (var _zkClient = new ZKClient(TestUtil.zkServers))
     {    
             await _zkClient.CreateRecursiveAsync("/zk/lock", null, CreateMode.Persistent);
        
             //创建分布式锁， 非线程安全类，每个线程请创建单独实例。
             var _lock = new ZKDistributedLock(_zkClient, "/zk/lock");
        
             await _lock.LockAsync(); //获得锁
        
             //do someting
        
             await _lock.UnLockAsync();//释放锁
    }
    
   
### Leader选举  
     using (var _zkClient = new ZKClient(TestUtil.zkServers))
     {          
             await _zkClient.CreateRecursiveAsync("/zk/leader", null, CreateMode.Persistent);
            
             var listener = new ZKLeaderSelectorListener();
             listener.takeLeadership = async (client, selector) =>
                                     {                 
                                         Console.WriteLine("I am the leader-" + await selector.GetLeaderAsync());
                                         selector.Close();
                                     };
             var selector = new ZKLeaderSelector("id", true, _zkClient, "/zk/leader", listener);
            //启动并参与Leader选举
             selector.Start();
            
             //获得当前主服务的ID
             await selector.GetLeaderAsync();
            
             //如果要退出Leader选举
             selector.Close();
    }
    
         
### 分布式队列   
     using (var _zkClient = new ZKClient(TestUtil.zkServers))
     {
             await _zkClient.CreateRecursiveAsync("/zk/queue", null, CreateMode.PERSISTENT);
        
             var queue = new ZKDistributedQueue<long>(new ZKClient(TestUtil.zkServers), "/zk/queue")
           
             await queue.OfferAsync("123");//放入元素
        
             var value = await queue.PollAsync();//删除并获取顶部元素
       
             var value =  await queue.PeekAsync(); //获取顶部元素，不会删除    
     }
    
	
	