# Overview
A zookeeper client, that makes life a little easier. Implemented by .Net. Reference https://github.com/sgroschupf/zkclient

# Quick Start
    ```
    public string ZKServers = "192.168.30.164:2181,192.168.30.165:2181,192.168.30.166:2181";

    public void CreateSession()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());

        Console.WriteLine("conneted ok!");
        zkClient.Close();
        zkClient = null;
    }

    public void CreateNode()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());

        Console.WriteLine("conneted ok!");

        User user = new User();
        user.Id = 1;
        user.Name = "testUser";

        string path = zkClient.Create("/testUserNode", user, CreateMode.Persistent);  
        Console.WriteLine("created path:" + path);
        zkClient.Close();
        zkClient = null;
    }

    public void GetData()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());
        Console.WriteLine("conneted ok!");

        Stat stat = new Stat();  
        User user = zkClient.ReadData<User>("/testUserNode", stat);
        Console.WriteLine(user.Name);
        Console.WriteLine(stat);
        zkClient.Close();
        zkClient = null;
    }

    public void Exists()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());
        Console.WriteLine("conneted ok!");

        bool e = zkClient.Exists("/testUserNode");
        Console.WriteLine(e);
        zkClient.Close();
        zkClient = null;
    }

    public void Delete()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());
        Console.WriteLine("conneted ok!");

        bool e1 = zkClient.Delete("/testUserNode"); 
        bool e2 = zkClient.DeleteRecursive("/test");

        Console.WriteLine(e1);
        Console.WriteLine(e2);
        zkClient.Close();
        zkClient = null;
    }

    public void WriteData()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());
        Console.WriteLine("conneted ok!");

        User user = new User();
        user.Id = 2;
        user.Name = "testUser2";

        zkClient.WriteData("/testUserNode", user);
        zkClient.Close();
        zkClient = null;
    }

    public void SubscribeChildChanges()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());
        Console.WriteLine("conneted ok!");

        zkClient.SubscribeChildChanges("/testUserNode3", new ZKChildListener());
        Thread.Sleep(int.MaxValue);
        zkClient.Close();
        zkClient = null;
    }

    public void SubscribeDataChanges()
    {
        ZkClient zkClient = new ZkClient(ZKServers, new TimeSpan(0, 0, 0, 0, 10000), new TimeSpan(0, 0, 0, 0, 10000), new SerializableSerializer());
        Console.WriteLine("conneted ok!");

        zkClient.SubscribeDataChanges("/testUserNode", new ZKDataListener());
        Thread.Sleep(int.MaxValue);
        zkClient.Close();
        zkClient = null;
    }
  
    [Serializable]
    public class User
    {
        public int Id { set; get; }

        public string Name { set; get; }
    }

    public class ZKChildListener : IZkChildListener
    {
        public void HandleChildChange(string parentPath, List<string> currentChilds)
        {
            Console.WriteLine(parentPath);
            Console.WriteLine(string.Join(".", currentChilds));
        }
    }

    public class ZKDataListener : IZkDataListener
    {
        public void HandleDataChange(string dataPath, object data)
        {
            Console.WriteLine(dataPath + ":" + Convert.ToString(data));
        }

        public void HandleDataDeleted(string dataPath)
        {
            Console.WriteLine(dataPath);
        }
    }
     ```