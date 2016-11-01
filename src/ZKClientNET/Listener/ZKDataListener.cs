using System;

namespace ZKClientNET.Listener
{
    public class ZKDataListener
    {
        private event Action<string, object> dataCreatedOrChangeEvent;

        private event Action<string, object> dataCreatedEvent;

        private event Action<string, object> dataChangeEvent;

        private event Action<string> dataDeletedEvent;


        public ZKDataListener DataCreatedOrChange(Action<string, object> dataCreatedOrChange)
        {
            dataCreatedOrChangeEvent += dataCreatedOrChange;
            return this;
        }

        public ZKDataListener DataCreated(Action<string, object> dataCreated)
        {
            dataCreatedEvent += dataCreated;
            return this;
        }

        public ZKDataListener DataChange(Action<string, object> dataChange)
        {
            dataChangeEvent += dataChange;
            return this;
        }

        public ZKDataListener DataDeleted(Action<string> dataDeleted)
        {
            dataDeletedEvent += dataDeleted;
            return this;
        }

        /// <summary>
        /// 节点创建和节点内容变化
        /// </summary>
        /// <param name="dataPath"></param>
        /// <param name="data"></param>
        public void HandleDataCreatedOrChange(string dataPath, object data)
        {
            dataCreatedOrChangeEvent(dataPath, data);
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