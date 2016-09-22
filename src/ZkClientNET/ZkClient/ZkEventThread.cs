using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient
{
    public abstract class ZkEvent
    {
        private string _description;

        public ZkEvent(string description)
        {
            _description = description;
        }

        public abstract void Run();


        public override string ToString()
        {
            return "ZkEvent[" + _description + "]";
        }
    }


    public class ZkEventThread
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZkEventThread));

        private BlockingCollection<ZkEvent> _events = new BlockingCollection<ZkEvent>(new ConcurrentQueue<ZkEvent>());

        private static int _eventId = 0;

    }
}
