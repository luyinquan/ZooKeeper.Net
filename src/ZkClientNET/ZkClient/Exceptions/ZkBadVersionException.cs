using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZkClientNET.ZkClient.Exceptions
{
    public class ZkBadVersionException : ZkException
    {
        public ZkBadVersionException() : base() { }

        public ZkBadVersionException(string message) : base(message) { }

        public ZkBadVersionException(KeeperException ex) : base(ex) { }

        public ZkBadVersionException(string message, KeeperException ex) : base(message, ex) { }
    }
}
