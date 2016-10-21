using System;
using System.Threading;
using ZkClientNET.Exceptions;

namespace ZkClientNET
{
    public class ExceptionUtil
    {
        //public static Exception convertToRuntimeException(Exception e)
        //{
        //    if (e as RuntimeException) {
        //        return (RuntimeException)e;
        //    }
        //    retainInterruptFlag(e);
        //    return new RuntimeException(e);
        //}

        public static Exception ConvertToRuntimeException(Exception e)
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
            if ((e as ZkInterruptedException) != null)
            {
                throw (ZkInterruptedException)e;
            }
        }

    }
}
