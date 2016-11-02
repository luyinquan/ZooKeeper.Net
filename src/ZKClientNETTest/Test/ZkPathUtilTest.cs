using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZKClientNET.Client;
using ZKClientNET.Util;
using ZKClientNETTest.Util;

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
