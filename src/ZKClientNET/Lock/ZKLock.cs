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
