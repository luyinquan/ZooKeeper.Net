using System.Collections.Generic;

namespace ZKClientNET.Listener
{
    public interface IZKChildListener
    {
        /// <summary>
        /// 子节点数量变化
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="currentChilds"></param>
        void HandleChildCountChanged(string parentPath, List<string> currentChilds);

        /// <summary>
        ///  子节点内容变化
        /// </summary>
        /// <param name="parentPath"></param>
        /// <param name="currentChilds"></param>
        void HandleChildChange(string parentPath, List<string> currentChilds);

    }
}
