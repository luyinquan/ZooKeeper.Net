using System;
using ZooKeeperNet;

namespace ZKClientNET.Exceptions
{
    public class ZKException : Exception
    {
        public Exception ex { set; get; }

        public ZKException() : base() { }

        public ZKException(string message) : base(message) { }

        public ZKException(Exception ex) : base(ex.Message, ex) { this.ex = ex; }

        public ZKException(string message, Exception ex) : base(message, ex) { }

        public static ZKException Create(KeeperException e)
        {
            switch (e.ErrorCode)
            {
                // case DATAINCONSISTENCY:
                // return new DataInconsistencyException();
                // case CONNECTIONLOSS:
                // return new ConnectionLossException();
                case KeeperException.Code.NONODE:
                    return new ZKNoNodeException(e);
                // case NOAUTH:
                // return new ZKNoAuthException();
                case KeeperException.Code.BADVERSION:
                    return new ZKBadVersionException(e);
                // case NOCHILDRENFOREPHEMERALS:
                // return new NoChildrenForEphemeralsException();
                case KeeperException.Code.NODEEXISTS:
                    return new ZKNodeExistsException(e);
                // case INVALIDACL:
                // return new ZKInvalidACLException();
                // case AUTHFAILED:
                // return new AuthFailedException();
                // case NOTEMPTY:
                // return new NotEmptyException();
                // case SESSIONEXPIRED:
                // return new SessionExpiredException();
                // case INVALIDCALLBACK:
                // return new InvalidCallbackException();
                default:
                    return new ZKException(e);
            }
        }
    }
}
