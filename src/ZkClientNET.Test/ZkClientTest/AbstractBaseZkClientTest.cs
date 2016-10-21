using log4net;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkClientNET.Exceptions;
using ZkClientNET.Util;

namespace ZkClientNET.Test.ZkClientTest
{
    public abstract class AbstractBaseZkClientTest
    {
        protected readonly ILog LOG = LogManager.GetLogger(typeof(AbstractBaseZkClientTest));
        protected ZkClient _zkClient;
        protected static string ip = ConfigurationManager.AppSettings["ip"].ToString();
        protected static string port = ConfigurationManager.AppSettings["port"].ToString();
    }
}
