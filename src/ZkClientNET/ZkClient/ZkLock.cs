using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient
{
    public class ZkLock
    {
        public object _dataChangedCondition = new object();
        public object _stateChangedCondition = new object();
        public object _zNodeEventCondition = new object();
    }
}
