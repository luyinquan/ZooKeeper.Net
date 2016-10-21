using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZkClientNET.Exceptions
{
    public class ZkNoNodeException : ZkException
    {
        public ZkNoNodeException() : base() { }

        public ZkNoNodeException(string message) : base(message) { }

        public ZkNoNodeException(KeeperException ex) : base(ex) { }

        public ZkNoNodeException(string message, KeeperException ex) : base(message, ex) { }

    }
}
