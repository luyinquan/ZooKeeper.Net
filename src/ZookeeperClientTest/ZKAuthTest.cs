using log4net;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ZookeeperClient.Client;
using ZookeeperClient.Util;
using static org.apache.zookeeper.ZooDefs;

namespace ZookeeperClient.Test
{
    public class ZKAuthTest
    {
        private static string testNode = "/test";
        private static string readAuth = "read-user:123456";
        private static string writeAuth = "write-user:123456";
        private static string deleteAuth = "delete-user:123456";
        private static string allAuth = "super-user:123456";
        private static string adminAuth = "admin-user:123456";
        private static string digest = "digest";

        protected ZKClient zkClient;
        protected static readonly ILog LOG = LogManager.GetLogger(typeof(ZKAuthTest));

        [OneTimeSetUp]
        public void SetUp()
        {
            LOG.Info("------------ BEFORE -------------");
            zkClient = new ZKClient(TestUtil.zkServers);

        }

        [OneTimeTearDown]
        public void TearDown()
        {
            LOG.Info("------------ AFTER -------------");
            zkClient.Close();
        }



        [Test]
        public async Task TestAuth()
        {
            zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(allAuth));
            if (await zkClient.ExistsAsync(testNode))
            {
                await zkClient.DeleteAsync(testNode);
            }
            List<ACL> acls = new List<ACL>();
            acls.Add(new ACL((int)Perms.ALL, new Id(digest, allAuth)));
            acls.Add(new ACL((int)Perms.READ, new Id(digest, readAuth)));
            acls.Add(new ACL((int)Perms.WRITE, new Id(digest, writeAuth)));
            acls.Add(new ACL((int)Perms.DELETE, new Id(digest, deleteAuth)));
            acls.Add(new ACL((int)Perms.ADMIN, new Id(digest, adminAuth)));

            await zkClient.CreatePersistentAsync(testNode, "test-data", acls);

            try
            {
                await zkClient.GetDataAsync<string>(testNode);//没有认证信息，读取会出错
            }
            catch (Exception e) { }

            try
            {
                zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(adminAuth));
                await zkClient.GetDataAsync<string>(testNode);//admin权限与read权限不匹配，读取也会出错
            }
            catch (Exception e) { }

            try
            {
                zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(readAuth));
                await zkClient.GetDataAsync<string>(testNode);//只有read权限的认证信息，才能正常读取
            }
            catch (Exception e) { }

            try
            {
                await zkClient.SetDataAsync(testNode, "new-data");//没有认证信息，写入会失败
            }
            catch (Exception e) { }

            try
            {
                zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(writeAuth));
                await zkClient.SetDataAsync(testNode, "new-data");//加入认证信息后,写入正常
            }
            catch (Exception e) { }

            try
            {
                zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(readAuth));
                await zkClient.GetDataAsync<string>(testNode);//读取新值验证
            }
            catch (Exception e) { }

            try
            {
                zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(deleteAuth));
                await zkClient.DeleteAsync(testNode);
            }
            catch (Exception e) { }

            //注：zkClient.setAcl方法查看源码可以发现，调用了readData、setAcl二个方法
            //所以要修改节点的ACL属性，必须同时具备read、admin二种权限        
            try
            {
                zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(adminAuth));
                zkClient.AddAuthInfo(digest, Encoding.Default.GetBytes(readAuth));

                List<ACL> acls1 = new List<ACL>();
                acls1.Add(new ACL((int)Perms.ALL, new Id(digest, adminAuth)));
                await zkClient.SetACLAsync(testNode, acls1);
                ACLResult aclResult = await zkClient.GetACLAsync(testNode);
            }
            catch (Exception e) { }
        }
    }
}
