using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZKClientNET.Exceptions
{
    public class ZKNoNodeException : ZKException
    {
        public ZKNoNodeException() : base() { }

        public ZKNoNodeException(string message) : base(message) { }

        public ZKNoNodeException(KeeperException ex) : base(ex) { }

        public ZKNoNodeException(string message, KeeperException ex) : base(message, ex) { }

    }
}
