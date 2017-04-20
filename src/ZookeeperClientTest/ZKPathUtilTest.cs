using NUnit.Framework;
using ZookeeperClient.Util;

namespace ZookeeperClient.Test
{
    public class ZKPathUtilTest
    {
        [Test]
        public void TestLeadingZeros()
        {
            Assert.True("0000000001" == ZKPathUtil.LeadingZeros(1, 10));
        }
    }
}
