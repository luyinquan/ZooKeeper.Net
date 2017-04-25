using org.apache.zookeeper.data;

namespace ZookeeperClient.Client
{
    public class ZKData<T>
    {
        public T data { set; get; }

        public Stat stat { set; get; }
    }
}
