using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET
{
    /// <summary>
    /// Updates the data of a znode. This is used together with {@link ZkClient#updateDataSerialized(String, DataUpdater)}.
    /// </summary>
    public interface IDataUpdater<T>
    {
        /// <summary>
        /// Updates the current data of a znode.
        /// </summary>
        /// <param name="currentData"></param>
        /// <returns></returns>
        T Update(T currentData);
    }
}
