using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZookeeperClient.Listener
{
    public class ZKChildListener: IZKChildListener
    {
        public Func<string, List<string>, Task> ChildCountChangedHandler { set; get; }

        public Func<string, List<string>, Task> ChildChangeHandler { set; get; }

    }
}
