using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZkClientNET.ZkClient.Exceptions
{
    public class ZkNodeExistsException : ZkException
    {
        public ZkNodeExistsException() : base() { }

        public ZkNodeExistsException(string message) : base(message) { }

        public ZkNodeExistsException(KeeperException ex) : base(ex) { }

        public ZkNodeExistsException(string message, KeeperException ex) : base(message, ex) { }
    }     
}
