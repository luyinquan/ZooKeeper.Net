using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZKClientNET.Lock
{
    public interface IZKLock
    {
        /// <summary>
        /// 获得锁
        /// </summary>
        /// <returns></returns>
        bool Lock();

        /// <summary>
        /// 释放锁
        /// </summary>
        /// <returns></returns>
        bool UnLock();
    }
}
