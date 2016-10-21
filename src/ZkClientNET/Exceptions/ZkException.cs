using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZkClientNET.Exceptions
{
    public class ZkException : Exception
    {
        public Exception ex { set; get; }

        public ZkException() : base() { }

        public ZkException(string message) : base(message) { }

        public ZkException(Exception ex) : base(ex.Message, ex) { this.ex = ex; }

        public ZkException(string message, Exception ex) : base(message, ex) { }

        public static ZkException Create(KeeperException e)
        {
            switch (e.ErrorCode)
            {
                // case DATAINCONSISTENCY:
                // return new DataInconsistencyException();
                // case CONNECTIONLOSS:
                // return new ConnectionLossException();
                case KeeperException.Code.NONODE:
                    return new ZkNoNodeException(e);
                // case NOAUTH:
                // return new ZkNoAuthException();
                case KeeperException.Code.BADVERSION:
                    return new ZkBadVersionException(e);
                // case NOCHILDRENFOREPHEMERALS:
                // return new NoChildrenForEphemeralsException();
                case KeeperException.Code.NODEEXISTS:
                    return new ZkNodeExistsException(e);
                // case INVALIDACL:
                // return new ZkInvalidACLException();
                // case AUTHFAILED:
                // return new AuthFailedException();
                // case NOTEMPTY:
                // return new NotEmptyException();
                // case SESSIONEXPIRED:
                // return new SessionExpiredException();
                // case INVALIDCALLBACK:
                // return new InvalidCallbackException();
                default:
                    return new ZkException(e);
            }
        }
    }
}
