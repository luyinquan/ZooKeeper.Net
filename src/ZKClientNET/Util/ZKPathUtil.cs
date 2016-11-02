using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZKClientNET.Client;

namespace ZKClientNET.Util
{
    public class ZKPathUtil
    {
        public static string LeadingZeros(long number, int numberOfLeadingZeros)
        {
            return number.ToString().PadLeft(numberOfLeadingZeros, '0');
        }   
    }
}
