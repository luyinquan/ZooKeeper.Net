using log4net;
using NUnit.Framework;
using Org.Apache.Zookeeper.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZkClientNET.Test.ZkClientTest
{
    public abstract class AbstractAuthTest
    {
        protected ZkClient _zkClient;

        protected static readonly ILog LOG = LogManager.GetLogger(typeof(AbstractAuthTest));
        protected static string ip = ConfigurationManager.AppSettings["ip"];
        protected static string port = ConfigurationManager.AppSettings["port"];
    }
}
