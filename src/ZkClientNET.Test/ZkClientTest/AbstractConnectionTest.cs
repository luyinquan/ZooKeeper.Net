using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZkClientNET.Util;
using ZooKeeperNet;

namespace ZkClientNET.Test.ZkClientTest
{
    public abstract class AbstractConnectionTest
    {
        protected static string ip = ConfigurationManager.AppSettings["ip"].ToString();
        protected static string port = ConfigurationManager.AppSettings["port"].ToString();
        protected IZkConnection _connection;

        public AbstractConnectionTest(IZkConnection connection)
        {
            _connection = connection;
        }
    }
}
