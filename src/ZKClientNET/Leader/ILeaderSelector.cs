using System.Collections.Generic;

namespace ZKClientNET.Leader
{
    public interface ILeaderSelector
    {
        /// <summary>
        /// 启动参与选举Leader
        /// </summary>
        void Start();

        /// <summary>
        /// 重新添加当前线程到选举队列
        /// </summary>
        void Requeue();

        /// <summary>
        /// 获得Leader ID
        /// </summary>
        /// <returns></returns>
        string GetLeader();

        /// <summary>
        /// 是否是Leader
        /// </summary>
        /// <returns></returns>
        bool IsLeader();

        /// <summary>
        /// 获得当前的所有参与者的路径名
        /// </summary>
        /// <returns></returns>
        List<string> GetParticipantNodes();


        /// <summary>
        /// 终止等待成为Leader
        /// </summary>
        void InterruptLeadership();

        /// <summary>
        /// 关闭Leader选举
        /// </summary>
        void Close();
    }
}
