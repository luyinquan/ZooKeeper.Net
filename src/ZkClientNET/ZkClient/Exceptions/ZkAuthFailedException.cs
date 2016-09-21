using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient.Exceptions
{
    public class ZkAuthFailedException : ZkException
    {
        public ZkAuthFailedException() : base() { }

        public ZkAuthFailedException(string message) : base(message) { }

        public ZkAuthFailedException(string message, Exception ex) : base(message, ex) { }

    }
}
