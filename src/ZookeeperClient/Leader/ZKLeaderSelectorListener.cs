using System;
using System.Threading.Tasks;
using ZookeeperClient.Client;

namespace ZookeeperClient.Leader
{
    public class ZKLeaderSelectorListener : IZKLeaderSelectorListener
    {
        public Func<ZKClient, ILeaderSelector, Task> takeLeadership;

        public void TakeLeadership(ZKClient zkClient, ILeaderSelector selector)
        {
            takeLeadership(zkClient, selector);
        }
    }
}
