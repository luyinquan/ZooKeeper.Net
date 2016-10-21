using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.Exceptions
{
    public class ZkMarshallingError : ZkException
    {
        public ZkMarshallingError() : base() { }

        public ZkMarshallingError(string message) : base(message) { }

        public ZkMarshallingError(Exception ex) : base(ex) { }
    }
}
