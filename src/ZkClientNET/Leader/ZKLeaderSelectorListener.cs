using System;
using ZKClientNET.Client;

namespace ZKClientNET.Leader
{
    public class ZKLeaderSelectorListener : IZKLeaderSelectorListener
    {
        private event Action<ZKClient, ILeaderSelector> takeLeadershipEvent;

        public ZKLeaderSelectorListener TakeLeadership(Action<ZKClient, ILeaderSelector> takeLeadership)
        {
            takeLeadershipEvent += takeLeadership;
            return this;
        }

        public void TakeLeadership(ZKClient client, ILeaderSelector selector)
        {
            takeLeadershipEvent(client, selector);
        }

    }
}
