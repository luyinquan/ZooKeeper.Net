using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZKClientNET.Listener
{

    public class ZKChildListener
    {       
        public event Action<string, List<string>> childCountChangedEvent;
       
        public event Action<string, List<string>> childChangeEvent;


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
