using System;
using System.Threading;
using ZKClientNET.Exceptions;

namespace ZKClientNET.Util
{
    public class ExceptionUtil
    {
        public static Exception ConvertToException(Exception e)
        {
            if ((e as Exception) != null)
            {
                return (Exception)e;
            }
            RetainInterruptFlag(e);
            return e;
        }

        /// <summary>
        ///   This sets the interrupt flag if the catched exception was an {@link InterruptedException}. Catching such an
        ///   exception always clears the interrupt flag.
        /// </summary>
        /// <param name="catchedException"></param>
        public static void RetainInterruptFlag(Exception catchedException)
        {
            if ((catchedException as ThreadInterruptedException) != null)
            {
                Thread.CurrentThread.Interrupt();
            }
        }

        public static void RethrowInterruptedException(Exception e)
        {
            if ((e as ThreadInterruptedException) != null)
            {
                throw (ThreadInterruptedException)e;
            }
            if ((e as ZKInterruptedException) != null)
            {
                throw (ZKInterruptedException)e;
            }
        }

    }
}
