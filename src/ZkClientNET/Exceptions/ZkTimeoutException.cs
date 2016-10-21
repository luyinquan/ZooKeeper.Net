using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.Exceptions
{
    public class ZkTimeoutException : ZkException
    {
        public ZkTimeoutException() : base() { }

        public ZkTimeoutException(string message) : base(message) { }

        public ZkTimeoutException(string message, Exception ex) : base(message, ex) { }
  
    }
}
