using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZkClientNET.Exceptions
{
    public class ZkInterruptedException : ZkException
    {
        public ZkInterruptedException(ThreadInterruptedException ex) : base(ex)
        {
            Thread.CurrentThread.Interrupt();
        }
    }
}
