using log4net;
using Org.Apache.Zookeeper.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkClientNET.ZkClient.Exceptions;
using ZkClientNET.ZkClient.Serialize;
using ZkClientNET.ZkClient.Util;
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
        private ConcurrentDictionary<string, ConcurrentHashSet<IZkChildListener>> _childListener = new ConcurrentDictionary<string, ConcurrentHashSet<IZkChildListener>>();
        private ConcurrentDictionary<string, ConcurrentHashSet<IZkDataListener>> _dataListener = new ConcurrentDictionary<string, ConcurrentHashSet<IZkDataListener>>();
        private  ConcurrentHashSet<IZkStateListener> _stateListener = new ConcurrentHashSet<IZkStateListener>();
        private KeeperState _currentState;
        private ZkLock _zkEventLock = new ZkLock();
        private bool _shutdownTriggered;
        private ZkEventThread _eventThread;
        // TODO PVo remove this later
        private Thread _zookeeperEventThread;
        private IZkSerializer _zkSerializer;
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

        public ZkClient(string zkServers, TimeSpan sessionTimeout, TimeSpan connectionTimeout, IZkSerializer zkSerializer, long operationRetryTimeout) : this(new ZkConnection(zkServers, sessionTimeout), connectionTimeout, zkSerializer, operationRetryTimeout)
        {

        }

        public ZkClient(IZkConnection connection) : this(connection, new TimeSpan(0, 0, 0, int.MaxValue))
        {

        }

        public ZkClient(IZkConnection connection, TimeSpan connectionTimeout) : this(connection, connectionTimeout, new SerializableSerializer())
        {

        }

        public ZkClient(IZkConnection zkConnection, TimeSpan connectionTimeout, IZkSerializer zkSerializer) : this(zkConnection, connectionTimeout, zkSerializer, -1)
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
        public ZkClient(IZkConnection zkConnection, TimeSpan connectionTimeout, IZkSerializer zkSerializer, long operationRetryTimeout)
        {
            if (zkConnection == null)
            {
                throw new ArgumentNullException("Zookeeper connection is null!");
            }
            _connection = zkConnection;
            _zkSerializer = zkSerializer;
            _operationRetryTimeoutInMillis = operationRetryTimeout;
            _isZkSaslEnabled = IsZkSaslEnabled();
            Connect(connectionTimeout, this);
        }

        public void SetZkSerializer(IZkSerializer zkSerializer)
        {
            _zkSerializer = zkSerializer;
        }

        public List<string> SubscribeChildChanges(string path, IZkChildListener listener)
        {
            ConcurrentHashSet<IZkChildListener> listeners = _childListener[path];
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZkChildListener>();
                _childListener.TryAdd(path, listeners);
            }
            listeners.Add(listener);
            return WatchForChilds(path);
        }

        public void SubscribeDataChanges(string path, IZkDataListener listener)
        {
            ConcurrentHashSet<IZkDataListener> listeners;
            listeners = _dataListener[path];
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZkDataListener>();
                _dataListener.TryAdd(path, listeners);
            }
            listeners.Add(listener);
            WatchForData(path);
            LOG.Debug("Subscribed data changes for " + path);
        }

        public void UnSubscribeDataChanges(string path, IZkDataListener dataListener)
        {
            ConcurrentHashSet<IZkDataListener> listeners = _dataListener[path];
            if (listeners != null)
            {
                listeners.Remove(dataListener);
            }
            if (listeners == null || listeners.IsEmpty()
            {
                ConcurrentHashSet<IZkDataListener> _listeners;
                _dataListener.TryRemove(path, out _listeners);
            }
        }

        public void SubscribeStateChanges( IZkStateListener listener)
        {
            _stateListener.Add(listener);
            
        }

        public void UnSubscribeStateChanges(IZkStateListener stateListener)
        {
            _stateListener.Remove(stateListener);
        }

        public void UnSubscribeAll()
        {
            _childListener.Clear();
            _dataListener.Clear();
            _stateListener.Clear();
        }

        /// <summary>
        ///  Updates data of an existing znode. The current content of the znode is passed to the {@link DataUpdater} that is
        /// passed into this method, which returns the new content.The new content is only written back to ZooKeeper if
        /// nobody has modified the given znode in between.If a concurrent change has been detected the new data of the
        ///znode is passed to the updater once again until the new contents can be successfully written back to ZooKeeper.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="updater"></param>
        public void UpdateDataSerialized<T>(string path, IDataUpdater<T> updater)
        {
            Stat stat = new Stat();
            bool retry;
            do
            {
                retry = false;
                try
                {
                    T oldData = (T)ReadData(path, stat);
                    T newData = updater.Update(oldData);
                    WriteData(path, newData, stat.Version);
                }
                catch (ZkBadVersionException e)
                {
                    retry = true;
                }
            } while (retry);
        }

        private void WriteData<T>(string path, T newData, int version)
        {
            throw new NotImplementedException();
        }

        public object ReadData(string path, Stat stat)
        {
            throw new NotImplementedException();
        }

        private void WatchForData(string path)
        {
            throw new NotImplementedException();
        }

        private List<string> WatchForChilds(string path)
        {
            throw new NotImplementedException();
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
