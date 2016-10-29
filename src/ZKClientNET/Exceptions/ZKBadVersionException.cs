using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZKClientNET.Exceptions
{
    public class ZKBadVersionException : ZKException
    {
        public ZKBadVersionException() : base() { }

        public ZKBadVersionException(string message) : base(message) { }

        public ZKBadVersionException(KeeperException ex) : base(ex) { }

        public ZKBadVersionException(string message, KeeperException ex) : base(message, ex) { }
    }
}
