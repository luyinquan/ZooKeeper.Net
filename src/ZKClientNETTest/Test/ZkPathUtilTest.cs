using NUnit.Framework;
using ZKClientNET.Util;

namespace ZKClientNETTest.Test
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
