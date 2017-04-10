using ZooKeeperNet;

namespace ZKClientNET.Exceptions
{
    public class ZKNodeExistsException : ZKException
    {
        public ZKNodeExistsException() : base() { }

        public ZKNodeExistsException(string message) : base(message) { }

        public ZKNodeExistsException(KeeperException ex) : base(ex) { }

        public ZKNodeExistsException(string message, KeeperException ex) : base(message, ex) { }
    }     
}
