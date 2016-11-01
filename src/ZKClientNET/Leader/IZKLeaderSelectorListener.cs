using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZKClientNET.Client;

namespace ZKClientNET.Leader
{
    public interface IZKLeaderSelectorListener
    {
        void TakeLeadership(ZKClient client, ILeaderSelector selector);
    }
}
