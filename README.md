## ZKClient
A zookeeper client, that makes life a little easier. Implemented by .Net. Reference https://github.com/sgroschupf/zkclient https://github.com/yuluows/zkclient 

## 使用说明
    ```
    /// <summary>
	/// 创建会话
	/// </summary>
	public void CreateSession()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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
	public void CreateNode()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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
	public void GetData()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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
	public void Exists()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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
	public void Delete()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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
	public void WriteData()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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
	public void SubscribeChildChanges()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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
	public void SubscribeDataChanges()
	{
		ZKClient zkClient = ZKClientBuilder.NewZKClient("localhost:2181")
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

    

    [Serializable]
    public class User
    {
        public int Id { set; Get; }

        public string Name { set; Get; }
    }
     ```
	 
## 扩展功能
    ###分布式锁

    ZKClient zkClient = ZKClientBuilder.NewZKClient()
                                .Servers("localhost:2181")
                                .Build();
    string lockPath = "/zk/lock";
    zkClient.CreateRecursive(lockPath, null, CreateMode.Persistent);
    //创建分布式锁， 非线程安全类，每个线程请创建单独实例。
    ZKDistributedLock _lock = ZKDistributedLock.NewInstance(zkClient,lockPath);  
    _lock.Lock(); //获得锁
    
    //do someting
    
    _lock.UnLock();//释放锁
    
###延迟获取分布式锁
网络闪断会引起短暂的网络断开，这个时间很短，但是却给分布式锁带来很大的麻烦。

例如线程1获得了分布式锁，但是却发生网络的短暂断开，如果这期间ZooKeeper服务器删除临时节点，分布式锁就会释放，其实线程1的工作一直在进行，并没有完成也没有宕机。

显然，由于网络短暂的断开引起的锁释放一般情况下不是我们想要的。所以提供了，具有延迟功能的分布式锁。

如果线程1获得了锁，并发生网络闪断，在ZK服务器删除临时节点后，那么其他线程并不会立即尝试获取锁，而是会等待一段时间，如果再这段时间内线程1成功连接上，那么线程1将继续持有锁。

    string lockPath = "/zk/delaylock";
    ZKClient zkClient1 = ZKClientBuilder.NewZKClient()
                            .Servers("localhost:2181")
                            .SessionTimeout(1000)
                            .Build();
    ZKDistributedDelayLock _lock = ZKDistributedDelayLock.NewInstance(zkClient1, lockPach);
    _lock.Lock(); //获得锁
    
    //do someting
    
    _lock.UnLock();//释放锁
    
###Leader选举
Leader选举是异步的，只需要调用selector.Start()就会启动并参与Leader选举，如果成为了主服务，则会执行监听器ZKLeaderSelectorListener。
    
    ZKClient zkClient = ZKClientBuilder.NewZKClient()
                                .Servers("localhost:2181")
                                .Build();
    string lockPath = "/zk/leader";
	ZKLeaderSelectorListener listener = new ZKLeaderSelectorListener()
                                   .TakeLeadership((client, selector) =>
                                   {
                                      //在这里可以编写，成为主服务后需要做的事情。
                                      Console.WriteLine("I am the leader-"+selector.GetLeader());
                                   });
    ZKLeaderSelector selector = new ZKLeaderSelector("service1", true, zkClient1, leaderPath, listener);
    //启动并参与Leader选举
    selector.Start();
    
    //获得当前主服务的ID
    selector.GetLeader();
    
    //如果要退出Leader选举
    selector.Close();
    
    
###延迟Leader选举
例如线程1被选举为Leader，但是却发生网络的短暂断开，如果zooKeeper服务器删除临时节点，其他线程会认为Leader宕机，会重新选举Leader，其实线程1的工作一直在继续并没有宕机。

显然，由于网络短暂的断开引起的这种情况不是我们需要的。

延迟Leader选举类是这样解决的，如果线程1成为了Leader，并发生网络闪断，在ZK服务器删除临时节点后，那么其他线程并不会立即竞争Leader，而是会等待一段时间。

如果再这段时间内线程1成功连接上，那么线程1保持Leader的角色。

    ZKClient zkClient = ZKClientBuilder.NewZKClient()
                                .Servers("localhost:2181")
                                .Build();
    string lockPath = "/zk/delayleader";
	ZKLeaderSelectorListener listener = new ZKLeaderSelectorListener()
                                   .TakeLeadership((client, selector) =>
                                   {  								   
                                     //在这里可以编写，成为主服务后需要做的事情。
                                      Console.WriteLine("I am the leader-"+selector.GetLeader());
                                   });
	
    //延迟3秒选举
    LeaderSelector selector = new ZKLeaderDelySelector("server1", true,3000, zkClient, leaderPath, listener);
    //启动并参与Leader选举
    selector.Start();
    
    //获得当前主服务的ID
    selector.GetLeader();
    
    //如果要退出Leader选举
    selector.Close();
    
    
###分布式队列
    
    ZKClient zkClient = ZKClientBuilder.NewZKClient()
                                .Servers("localhost:2181")
                                .Build();
    string rootPath = "/zk/queue";
    zkClient.CreateRecursive(rootPath, null, CreateMode.Persistent);
    
    //创建分布式队列对象
    ZKDistributedQueue<string> queue = new ZKDistributedQueue(zkClient, rootPath);
    
    queue.Offer("123");//放入元素
    
    string value = queue.Poll();//删除并获取顶部元素
   
    string value =  queue.Peek(); //获取顶部元素，不会删除
    
###主从服务锁

    ZKClient zkClient = ZKClientBuilder.NewZKClient()
                                .Servers("localhost:2181")
                                .Build();
    string lockPath = "/zk/halock";
    zkClient.CreateRecursive(rootPath, null, CreateMode.Persistent);
    
    //创建锁， 非线程安全类，每个线程请创建单独实例。
    ZKHALock _lock = ZKHALock.NewInstance(zkClient, lockPach);
    
    _lock.Lock();//尝试获取锁
    
    //获取锁成功，当前线程变为主服务。
    //直到主服务宕机或与zk服务端断开连接，才会释放锁。
    //此时从服务尝试获得锁，选取一个从服务变为主服务

	
	