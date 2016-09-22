using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient
{
    public class ZkServer
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZkServer));

        public const int DEFAULT_PORT = 2181;
        public const int DEFAULT_TICK_TIME = 5000;
        public const int DEFAULT_MIN_SESSION_TIMEOUT = 2 * DEFAULT_TICK_TIME;

        private string _dataDir;
        private string _logDir;

        private IDefaultNameSpace _defaultNameSpace;
        private ZooKeeperServer _zk;
        private NIOServerCnxnFactory _nioFactory;
        private ZkClient _zkClient;
        private int _port;
        private int _tickTime;
        private int _minSessionTimeout;

        public ZkServer(string dataDir, string logDir, IDefaultNameSpace defaultNameSpace) :this(dataDir, logDir, defaultNameSpace, DEFAULT_PORT)
        {
        }

        public ZkServer(string dataDir, string logDir, IDefaultNameSpace defaultNameSpace, int port) : this(dataDir, logDir, defaultNameSpace, port, DEFAULT_TICK_TIME)
        {       
        }

        public ZkServer(string dataDir, string logDir, IDefaultNameSpace defaultNameSpace, int port, int tickTime) : this(dataDir, logDir, defaultNameSpace, port, tickTime, DEFAULT_MIN_SESSION_TIMEOUT)
        {          
        }

        public ZkServer(string dataDir, string logDir, IDefaultNameSpace defaultNameSpace, int port, int tickTime, int minSessionTimeout)
        {
            _dataDir = dataDir;
            _logDir = logDir;
            _defaultNameSpace = defaultNameSpace;
            _port = port;
            _tickTime = tickTime;
            _minSessionTimeout = minSessionTimeout;
        }
    }
}
