namespace ZKClientNET.Client
{
    /// <summary>
    /// Updates the data of a znode. This is used together with {@link ZKClientNET#updateDataSerialized(String, DataUpdater)}.
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
