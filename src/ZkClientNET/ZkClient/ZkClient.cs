using log4net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkClientNET.ZkClient.Serialize;
using ZooKeeperNet;

namespace ZkClientNET.ZkClient
{
    /// <summary>
    /// Abstracts the interaction with zookeeper and allows permanent (not just one time) watches on nodes in ZooKeeper
    /// </summary>
    public class ZkClient : IWatcher
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZkClient));

        protected const string JAVA_LOGIN_CONFIG_PARAM = "java.security.auth.login.config";
        protected const string ZK_SASL_CLIENT = "zookeeper.sasl.client";
        protected const string ZK_LOGIN_CONTEXT_NAME_KEY = "zookeeper.sasl.clientconfig";

        protected IZkConnection _connection;
        protected long _operationRetryTimeoutInMillis;
        private ConcurrentDictionary<string, HashSet<IZkChildListener>> _childListener = new ConcurrentDictionary<string, HashSet<IZkChildListener>>();
        private ConcurrentDictionary<string, HashSet<IZkDataListener>> _dataListener = new ConcurrentDictionary<string, HashSet<IZkDataListener>>();
        // private  HashSet<IZkStateListener> _stateListener = new CopyOnWriteArrayHashSet<IZkStateListener>();
        private KeeperState _currentState;
        private ZkLock _zkEventLock = new ZkLock();
        private bool _shutdownTriggered;
        private ZkEventThread _eventThread;
        // TODO PVo remove this later
        private Thread _zookeeperEventThread;
        private IZkSerializer _IZkSerializer;
        private volatile bool _closed;
        private bool _isZkSaslEnabled;

        public ZkClient(string serverstring) : this(serverstring, new TimeSpan(0, 0, 0, int.MaxValue))
        {

        }

        public ZkClient(string zkServers, TimeSpan connectionTimeout) : this(new ZkConnection(zkServers), connectionTimeout)
        {

        }

        public ZkClient(string zkServers, TimeSpan sessionTimeout, TimeSpan connectionTimeout) : this(new ZkConnection(zkServers, sessionTimeout), connectionTimeout)
        {

        }

        public ZkClient(string zkServers, TimeSpan sessionTimeout, TimeSpan connectionTimeout, IZkSerializer zkSerializer) : this(new ZkConnection(zkServers, sessionTimeout), connectionTimeout, zkSerializer)
        {

        }

        /// <summary>
        /// Most operations done through this {@link org.I0Itec.zkclient.ZkClient}
        /// are retried in cases like
        /// connection loss with the Zookeeper servers.During such failures, this
        /// <code>operationRetryTimeout</code> decides the maximum amount of time, in milli seconds, each
        ///operation is retried.A value lesser than 0 is considered as
        ///"retry forever until a connection has been reestablished".
        /// </summary>
        /// <param name="zkServers">  The Zookeeper servers</param>
        /// <param name="sessionTimeout">The session timeout in milli seconds</param>
        /// <param name="connectionTimeout">The connection timeout in milli seconds</param>
        /// <param name="IZkSerializer"> The Zookeeper data serializer</param>
        /// <param name="operationRetryTimeout"> operationRetryTimeout</param>

        public ZkClient(string zkServers, TimeSpan sessionTimeout, TimeSpan connectionTimeout, IZkSerializer IZkSerializer, long operationRetryTimeout) : this(new ZkConnection(zkServers, sessionTimeout), connectionTimeout, IZkSerializer, operationRetryTimeout)
        {

        }

        public ZkClient(IZkConnection connection) : this(connection, new TimeSpan(0, 0, 0, int.MaxValue))
        {

        }

        public ZkClient(IZkConnection connection, TimeSpan connectionTimeout) : this(connection, connectionTimeout, new SerializableSerializer())
        {

        }

        public ZkClient(IZkConnection zkConnection, TimeSpan connectionTimeout, IZkSerializer ZkSerializer) : this(zkConnection, connectionTimeout, ZkSerializer, -1)
        {

        }

        /// <summary>
        /// Most operations done through this {@link org.I0Itec.zkclient.ZkClient} are retried in cases like
        /// connection loss with the Zookeeper servers.During such failures, this
        /// <code>operationRetryTimeout</code> decides the maximum amount of time, in milli seconds, each
        /// operation is retried.A value lesser than 0 is considered as
        /// retry forever until a connection has been reestablished".
        /// </summary>
        /// <param name="zkConnection"> The Zookeeper servers</param>
        /// <param name="connectionTimeout"> The connection timeout in milli seconds</param>
        /// <param name="IZkSerializer"></param>
        /// <param name="operationRetryTimeout"> The Zookeeper data serializer</param>
        public ZkClient(IZkConnection zkConnection, TimeSpan connectionTimeout, IZkSerializer IZkSerializer, long operationRetryTimeout)
        {
            if (zkConnection == null)
            {
                throw new ArgumentNullException("Zookeeper connection is null!");
            }
            _connection = zkConnection;
            _IZkSerializer = IZkSerializer;
            _operationRetryTimeoutInMillis = operationRetryTimeout;
            _isZkSaslEnabled = IsZkSaslEnabled();
            Connect(connectionTimeout, this);
        }


        private void Connect(TimeSpan connectionTimeout, ZkClient zkClient)
        {
            throw new NotImplementedException();
        }

        private bool IsZkSaslEnabled()
        {
            throw new NotImplementedException();
        }

        public void Process(WatchedEvent @event)
        {
            throw new NotImplementedException();
        }
    }
}
