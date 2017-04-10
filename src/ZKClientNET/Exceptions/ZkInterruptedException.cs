using System.Threading;

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
