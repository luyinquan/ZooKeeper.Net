using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZKClientNET.Client
{
    public class ZKNode
    {
        private object p;
        private CreateMode ephemeral;

        public ZKNode()
        { }

        public ZKNode(string path, object p, CreateMode ephemeral)
        {
            this.path = path;
            this.p = p;
            this.ephemeral = ephemeral;
        }

        public string path { set; get; }

        public object data { set; get; }

        public CreateMode createMode { set; get; }
    }
}
