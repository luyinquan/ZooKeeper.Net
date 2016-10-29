using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZKClientNET.Exceptions
{
    public class ZKMarshallingError : ZKException
    {
        public ZKMarshallingError() : base() { }

        public ZKMarshallingError(string message) : base(message) { }

        public ZKMarshallingError(Exception ex) : base(ex) { }
    }
}
