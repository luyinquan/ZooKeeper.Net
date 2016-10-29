using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZKClientNET.Exceptions
{
    public class ZKInterruptedException : ZKException
    {
        public ZKInterruptedException(ThreadInterruptedException ex) : base(ex)
        {
            Thread.CurrentThread.Interrupt();
        }
    }
}
