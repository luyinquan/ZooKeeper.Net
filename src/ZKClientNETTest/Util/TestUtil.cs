using Moq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZKClientNET.Client;

namespace ZKClientNETTest.Util
{
    public class TestUtil
    {
        //zk集群的地址  
        public static string zkServers = ConfigurationManager.AppSettings["zkServers"];

        public static string ip = ConfigurationManager.AppSettings["ip"];

        public static string port = ConfigurationManager.AppSettings["port"];

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

        public static void ReSetPathCreate(ZKClient _zkClient, string path)
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

        public static void ReSetPathUnCreate(ZKClient _zkClient, string path)
        {
            var children = _zkClient.GetChildren("/");
            children.ForEach(x =>
            {
                _zkClient.DeleteRecursive("/" + x);
            });
            if (_zkClient.Exists(path))
            {
                _zkClient.DeleteRecursive(path);
            }
        }

    }
}
