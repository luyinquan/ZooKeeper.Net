using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZKClientNET.Client;

namespace ZKClientNET.Leader
{
    public class ZKLeaderSelectorListener
    {
        public event Action<ZKClient, ILeaderSelector> takeLeadershipEvent;

        public void TakeLeadership(ZKClient client, ILeaderSelector selector)
        {
            takeLeadershipEvent(client, selector);
        }

    }
}
