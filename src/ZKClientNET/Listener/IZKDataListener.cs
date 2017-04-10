namespace ZKClientNET.Listener
{
    public interface IZKDataListener
    {
        /// <summary>
        /// 节点创建和节点内容变化
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="data"></param>
        void HandleDataCreatedOrChange(string dataPath, object data);

        /// <summary>
        /// 节点创建
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="data"></param>
        void HandleDataCreated(string dataPath, object data);

        /// <summary>
        /// 节点内容变化
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="data"></param>
        void HandleDataChange(string dataPath, object data);

        /// <summary>
        /// 节点删除
        /// </summary>
        /// <param name="dataPath"></param>
        void HandleDataDeleted(string dataPath);
    }
}
