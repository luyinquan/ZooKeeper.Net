using System;

namespace ZKClientNET.Listener
{
    public class ZKDataListener
    {
        public event Action<string, object> dataChangesEvent;

        public event Action<string, object> dataCreatedEvent;

        public event Action<string, object> dataChangeEvent;

        public event Action<string> dataDeletedEvent;

        /// <summary>
        /// 节点创建和节点内容变化
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="data"></param>
        public void HandleDataChanges(string dataPath, object data)
        {
            dataChangesEvent(dataPath, data);
        }

        /// <summary>
        /// 节点创建
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="data"></param>
        public void HandleDataCreated(string dataPath, object data)
        {
            dataCreatedEvent(dataPath, data);
        }
     
        /// <summary>
        /// 节点内容变化
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="data"></param>
        public void HandleDataChange(string dataPath, object data)
        {
            dataChangeEvent(dataPath, data);
        }

        /// <summary>
        /// 节点删除
        /// </summary>
        /// <param name="dataPath"></param>
        public void HandleDataDeleted(string dataPath)
        {
            dataDeletedEvent(dataPath);
        }
    }
}