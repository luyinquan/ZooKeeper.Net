using log4net;
using NUnit.Framework;
using org.apache.zookeeper;
using System.Text;
using System.Threading.Tasks;
using ZookeeperClient.Client;
using ZookeeperClient.Util;
using static org.apache.zookeeper.ZooDefs;

namespace ZookeeperClient.Test
{
    public  class ZKAuthTest
    {
        protected ZKClient _zkClient;
        protected static readonly ILog LOG = LogManager.GetLogger(typeof(ZKAuthTest));

        [OneTimeSetUp]
        public  void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            _zkClient = new ZKClient(TestUtil.zkServers);
           
        }

        [OneTimeTearDown]
        public  void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            _zkClient.Close();
        }

        [Test]
        public async Task TestAuthorized()
        {
            _zkClient.AddAuthInfo("digest", Encoding.Default.GetBytes("pat:pass"));
            await TestUtil.ReSetPathUnCreate(_zkClient, "/path1");
            await _zkClient.CreateAsync("/path1", null, Ids.CREATOR_ALL_ACL, CreateMode.PERSISTENT);
            await _zkClient.GetDataAsync<object>("/path1");
        }

        [Test]
        public async Task TestSetAndGetAcls()
        {
            _zkClient.AddAuthInfo("digest", Encoding.Default.GetBytes("pat:pass"));
            await TestUtil.ReSetPathUnCreate(_zkClient, "/path1");
            await _zkClient.CreateAsync("/path1", null, Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
            Assert.True((await _zkClient.GetACLAsync("/path1")).Acls.Count == Ids.OPEN_ACL_UNSAFE.Count);

            for (int i = 0; i < 100; i++)
            {
                await _zkClient.SetACLAsync("/path1", Ids.OPEN_ACL_UNSAFE);
                Assert.True((await _zkClient.GetACLAsync("/path1")).Acls.Count == Ids.OPEN_ACL_UNSAFE.Count);
            }
        }
    }
}
