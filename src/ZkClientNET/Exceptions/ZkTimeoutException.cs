using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZKClientNET.Exceptions
{
    public class ZKTimeoutException : ZKException
    {
        public ZKTimeoutException() : base() { }

        public ZKTimeoutException(string message) : base(message) { }

        public ZKTimeoutException(string message, Exception ex) : base(message, ex) { }
  
    }
}
