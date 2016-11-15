using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZKClientNET.Listener
{

    public class ZKChildListener : IZKChildListener
    {
        private event Action<string, List<string>> childCountChangedEvent;

        private event Action<string, List<string>> childChangeEvent;

        public ZKChildListener ChildCountChanged(Action<string, List<string>> childCountChanged)
        {
            childCountChangedEvent += childCountChanged;
            return this;
        }

        public ZKChildListener ChildChange(Action<string, List<string>> childChange)
        {
            childChangeEvent += childChange;
            return this;
        }

        /// <summary>
        /// 子节点数量变化
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="currentChilds"></param>
        public void HandleChildCountChanged(string parentPath, List<string> currentChilds)
        {
            childCountChangedEvent(parentPath, currentChilds);
        }

        /// <summary>
        ///  子节点内容变化
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="currentChilds"></param>
        public void HandleChildChange(string parentPath, List<string> currentChilds)
        {
            childChangeEvent(parentPath, currentChilds);
        }
    }
}
