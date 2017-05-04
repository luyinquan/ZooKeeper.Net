using Xunit;
using ZooKeeperClient.Util;

namespace ZooKeeperClient.Test
{
    public class ZKPathUtilTest
    {
        [Fact]
        public void TestLeadingZeros()
        {
            Assert.True("0000000001" == ZKPathUtil.LeadingZeros(1, 10));
        }
    }
}
