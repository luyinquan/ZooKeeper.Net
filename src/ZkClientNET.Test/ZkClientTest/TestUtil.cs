using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZkClientNET.Test.ZkClientTest
{
    public class TestUtil
    {
         /// <summary>
        /// This waits until the provided {@link Callable} returns an object that is equals to the given expected value or
        /// the timeout has been reached.In both cases this method will return the return value of the latest
        ///{@link Callable}
        ///execution.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expectedValue"></param>
        /// <param name="callable"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static T WaitUntil<T>(T expectedValue, Func<T> callable, TimeSpan timeout)
        {
            var now = DateTime.Now;
            do
            {
                T actual = callable();
                if (expectedValue.Equals(actual))
                {
                    return actual;
                }
                if ((DateTime.Now - now) > timeout)
                {
                    return actual;
                }
                Thread.Sleep(50);
            } while (true);

        }

        public static void WaitUntilVerified<T>(Action runnable, TimeSpan timeout)
        {
            var now = DateTime.Now;
            do
            {
                MockException exception = null;
                try
                {
                    runnable();
                }
                catch (MockException e)
                {
                    exception = e;
                }
                if (exception == null)
                {
                    return;
                }
                if ((DateTime.Now - now) > timeout)
                {
                    throw exception;
                }
                Thread.Sleep(50);
            } while (true);
        }

        public static void ReSetPathCreate(ZkClient _zkClient, string path)
        {
            if (!_zkClient.Exists(path))
            {
                _zkClient.CreatePersistent(path);
            }
            else
            {
                _zkClient.DeleteRecursive(path);
                _zkClient.CreatePersistent(path);
            }
        }

        public static void ReSetPathUnCreate(ZkClient _zkClient, string path)
        {
            if (_zkClient.Exists(path))
            {
                _zkClient.DeleteRecursive(path);
            }
        }

    }
}
