using ZKClientNET.Client;

namespace ZKClientNET.Leader
{
    public interface IZKLeaderSelectorListener
    {
        void TakeLeadership(ZKClient client, ILeaderSelector selector);
    }
}
