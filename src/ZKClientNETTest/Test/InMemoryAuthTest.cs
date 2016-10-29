using log4net;
using NUnit.Framework;
using Org.Apache.Zookeeper.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Connection;
using ZKClientNETTest.Util;
using ZooKeeperNet;

namespace ZKClientNETTest.Test
{
    public class InMemoryAuthTest 
    {
        protected ZKClient _zkClient;
        protected static readonly ILog LOG = LogManager.GetLogger(typeof(InMemoryAuthTest));

        [OneTimeSetUp]
        public void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(new InMemoryConnection());
            TestUtil.ReSetPathUnCreate(_zkClient, "/path1");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        public void TestAuthorized()
        {
            List<ACL> acl = new List<ACL>();
            acl.Add(new ACL(Perms.ALL, new ZKId("digest", "pat:pass")));
            _zkClient.AddAuthInfo("digest", Encoding.Default.GetBytes("pat:pass"));
            _zkClient.Create("/path1", null, acl, CreateMode.Persistent);
            _zkClient.ReadData<object>("/path1");
        }

        [Test]
        public virtual void TestBadAuth()
        {
            try
            {
                List<ACL> acl = new List<ACL>();
                acl.Add(new ACL(Perms.ALL, new ZKId("digest", "pat:pass")));
                _zkClient.Create("/path1", null, acl, CreateMode.Ephemeral);
                _zkClient.AddAuthInfo("digest", Encoding.Default.GetBytes("pat:pass2"));
                _zkClient.ReadData<object>("/path1");
                Assert.Fail("Should get auth error");
            }
            catch (Exception e)
            {
                if ((e as KeeperException) != null && (((KeeperException)e).ErrorCode == KeeperException.Code.NOAUTH))
                {
                    // do nothing, this is expected
                }
                else
                {
                    Assert.Fail("wrong exception");
                }
            }
        }

    }
}
