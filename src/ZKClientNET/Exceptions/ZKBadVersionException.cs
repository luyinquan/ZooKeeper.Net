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
