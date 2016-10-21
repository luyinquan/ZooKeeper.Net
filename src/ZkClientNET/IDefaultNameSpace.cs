using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET
{
    public interface IDefaultNameSpace
    {
        /// <summary>
        /// Creates a set of default folder structure within a zookeeper .
        /// </summary>
        /// <param name="zkClient"></param>
        void CreateDefaultNameSpace(ZkClient zkClient);
    }
}
