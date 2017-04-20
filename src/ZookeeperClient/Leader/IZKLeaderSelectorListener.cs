using ZookeeperClient.Client;

namespace ZookeeperClient.Leader
{
    public interface IZKLeaderSelectorListener
    {
        void TakeLeadership(ZKClient zkClient, ILeaderSelector selector);
    }
}
