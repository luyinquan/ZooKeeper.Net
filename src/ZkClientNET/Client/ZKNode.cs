using ZooKeeperNet;

namespace ZKClientNET.Client
{
    public class ZKNode
    {
        public ZKNode()
        { }

        public ZKNode(string path, object data, CreateMode createMode)
        {
            this.path = path;
            this.data = data;
            this.createMode = createMode;
        }

        public string path { set; get; }

        public object data { set; get; }

        public CreateMode createMode { set; get; }
    }
}
