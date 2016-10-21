using log4net;
using Org.Apache.Zookeeper.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ZkClientNET.Exceptions;
using ZkClientNET.Serialize;
using ZkClientNET.Util;
using ZooKeeperNet;
using static ZooKeeperNet.KeeperException;

namespace ZkClientNET
{
    /// <summary>
    /// Abstracts the interaction with zookeeper and allows permanent (not just one time) watches on nodes in ZooKeeper
    /// </summary>
    public class ZkClient : IWatcher
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZkClient));

        protected IZkConnection _connection;
        protected long _operationRetryTimeoutInMillis;
        private ConcurrentDictionary<string, ConcurrentHashSet<IZkChildListener>> _childListener = new ConcurrentDictionary<string, ConcurrentHashSet<IZkChildListener>>();
        private ConcurrentDictionary<string, ConcurrentHashSet<IZkDataListener>> _dataListener = new ConcurrentDictionary<string, ConcurrentHashSet<IZkDataListener>>();
        private ConcurrentHashSet<IZkStateListener> _stateListener = new ConcurrentHashSet<IZkStateListener>();
        private KeeperState _currentState;
        private object _zkEventLock = new object();
        private bool _shutdownTriggered;
        //private ZkEventThread _eventThread;
        private ZkTask _eventTask;
        // TODO PVo remove this later
        private Thread _zookeeperEventThread;
        private IZkSerializer _zkSerializer;
        private volatile bool _closed;
        //private bool _isZkSaslEnabled;

        #region ZkClient
        public ZkClient(string serverstring) : this(serverstring, new TimeSpan(0, 0, 0, 0, int.MaxValue))
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

        public ZkClient(IZkConnection connection) : this(connection, new TimeSpan(0, 0, 0, 0, int.MaxValue))
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
            //_isZkSaslEnabled = IsZkSaslEnabled();
            Connect(connectionTimeout, this);
        }
        #endregion

        public void SetZkSerializer(IZkSerializer zkSerializer)
        {
            _zkSerializer = zkSerializer;
        }

        public List<string> SubscribeChildChanges(string path, IZkChildListener listener)
        {
            //Monitor.Enter(_childListener);
            ConcurrentHashSet<IZkChildListener> listeners;
            _childListener.TryGetValue(path, out listeners);
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZkChildListener>();
                _childListener.TryAdd(path, listeners);
            }
            listeners.Add(listener);
            //Monitor.Exit(_childListener);
            return WatchForChilds(path);
        }

        public void UnSubscribeChildChanges(string path, IZkChildListener childListener)
        {
            //Monitor.Enter(_childListener);
            ConcurrentHashSet<IZkChildListener> listeners;
            _childListener.TryGetValue(path, out listeners);
            if (listeners != null)
            {
                listeners.Remove(childListener);
            }
            //Monitor.Exit(_childListener);
        }

        public void SubscribeDataChanges(string path, IZkDataListener listener)
        {
            ConcurrentHashSet<IZkDataListener> listeners;
            //Monitor.Enter(_dataListener);
            _dataListener.TryGetValue(path, out listeners);
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZkDataListener>();
                _dataListener.TryAdd(path, listeners);
            }
            listeners.Add(listener);
            //Monitor.Exit(_dataListener);
            WatchForData(path);
            LOG.Debug("Subscribed data changes for " + path);
        }

        public void UnSubscribeDataChanges(string path, IZkDataListener dataListener)
        {
            //Monitor.Enter(_dataListener);
            ConcurrentHashSet<IZkDataListener> listeners;
            _dataListener.TryGetValue(path, out listeners);
            if (listeners != null)
            {
                listeners.Remove(dataListener);
            }
            if (listeners == null || listeners.IsEmpty)
            {
                ConcurrentHashSet<IZkDataListener> _listeners;
                _dataListener.TryRemove(path, out _listeners);
            }
            //Monitor.Exit(_dataListener);
        }

        public void SubscribeStateChanges(IZkStateListener listener)
        {
            //Monitor.Enter(_stateListener);
            _stateListener.Add(listener);
            //Monitor.Exit(_stateListener);
        }

        public void UnSubscribeStateChanges(IZkStateListener stateListener)
        {
            //Monitor.Enter(_stateListener);
            _stateListener.Remove(stateListener);
            //Monitor.Exit(_stateListener);
        }

        public void UnSubscribeAll()
        {
            //Monitor.Enter(_childListener);
            _childListener.Clear();
            //Monitor.Exit(_childListener);

            //Monitor.Enter(_dataListener);
            _dataListener.Clear();
            //Monitor.Exit(_dataListener);

            //Monitor.Enter(_stateListener);
            _stateListener.Clear();
            //Monitor.Exit(_stateListener);
        }

        /// <summary>
        /// Create a persistent node.
        /// </summary>
        /// <param name="path"></param>
        public void CreatePersistent(string path)
        {
            CreatePersistent(path, false);
        }

        /// <summary>
        /// Create a persistent node and set its ACLs.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="createParents"></param>
        private void CreatePersistent(string path, bool createParents)
        {
            CreatePersistent(path, createParents, Ids.OPEN_ACL_UNSAFE);
        }

        /// <summary>
        /// Create a persistent node and set its ACLs.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="createParents"></param>
        /// <param name="acl"></param>
        private void CreatePersistent(string path, bool createParents, List<ACL> acl)
        {
            try
            {
                Create(path, null, acl, CreateMode.Persistent);
            }
            catch (ZkNodeExistsException e)
            {
                if (!createParents)
                {
                    throw e;
                }
            }
            catch (ZkNoNodeException e)
            {
                if (!createParents)
                {
                    throw e;
                }
                string parentDir = path.Substring(0, path.LastIndexOf('/'));
                CreatePersistent(parentDir, createParents, acl);
                CreatePersistent(path, createParents, acl);
            }
        }

        /// <summary>
        /// Sets the acl on path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="acl"></param>
        public void SetAcl(string path, List<ACL> acl)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Missing value for path");
            }

            if (acl == null || acl.Count == 0)
            {
                throw new ArgumentNullException("Missing value for ACL");
            }

            if (!Exists(path))
            {
                throw new Exception("trying to set acls on non existing node " + path);
            }

            RetryUntilConnected(() =>
            {
                Stat stat = new Stat();
                _connection.ReadData(path, stat, false);
                _connection.SetACL(path, acl, stat.Aversion);
                return default(KeyValuePair<List<ACL>, Stat>); ;
            });
        }

        public KeyValuePair<List<ACL>, Stat> GetAcl(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Missing value for path");
            }

            if (!Exists(path))
            {
                throw new Exception("trying to get acls on non existing node " + path);
            }

            return RetryUntilConnected(() =>
           {
               return _connection.GetACL(path);
           });
        }

        /// <summary>
        /// Create a persistent node.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        public void CreatePersistent(string path, object data)
        {
            Create(path, data, CreateMode.Persistent);
        }

        /// <summary>
        /// Create a persistent node.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="acl"></param>
        public void CreatePersistent(string path, object data, List<ACL> acl)
        {
            Create(path, data, acl, CreateMode.Persistent);
        }

        /// <summary>
        /// reate a persistent, sequental node.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string CreatePersistentSequential(string path, object data)
        {
            return Create(path, data, CreateMode.PersistentSequential);
        }

        /// <summary>
        /// Create a persistent, sequential node and set its ACL.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="acl"></param>
        /// <returns></returns>
        public string CreatePersistentSequential(string path, object data, List<ACL> acl)
        {
            return Create(path, data, acl, CreateMode.PersistentSequential);
        }

        /// <summary>
        /// Create an ephemeral node.
        /// </summary>
        /// <param name="path"></param>
        public void CreateEphemeral(string path)
        {
            Create(path, null, CreateMode.Ephemeral);
        }

        /// <summary>
        /// Create an ephemeral node and set its ACL.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="acl"></param>
        public void CreateEphemeral(string path, List<ACL> acl)
        {
            Create(path, null, acl, CreateMode.Ephemeral);
        }

        /// <summary>
        /// Create a node.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="persistent"></param>
        /// <returns></returns>
        public string Create(string path, object data, CreateMode mode)
        {
            return Create(path, data, Ids.OPEN_ACL_UNSAFE, mode);
        }

        /// <summary>
        /// Create a node with ACL.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="p"></param>
        /// <param name="acl"></param>
        /// <param name="persistent"></param>
        /// <returns></returns>
        public string Create(string path, object data, List<ACL> acl, CreateMode mode)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Missing value for path");
            }
            if (acl == null || acl.Count == 0)
            {
                throw new ArgumentNullException("Missing value for ACL");
            }

            byte[] bytes = data == null ? null : Serialize(data);

            return RetryUntilConnected(() =>
            {
                return _connection.Create(path, bytes, acl, mode);
            });

        }

        /// <summary>
        /// Create an ephemeral node.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        public void CreateEphemeral(string path, object data)
        {
            Create(path, data, CreateMode.Ephemeral);
        }

        /// <summary>
        /// Create an ephemeral, sequential node.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string CreateEphemeralSequential(string path, object data)
        {
            return Create(path, data, CreateMode.EphemeralSequential);
        }

        /// <summary>
        /// Create an ephemeral, sequential node with ACL.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="acl"></param>
        /// <returns></returns>
        public string CreateEphemeralSequential(string path, object data, List<ACL> acl)
        {
            return Create(path, data, acl, CreateMode.EphemeralSequential);
        }

        private void FireAllEvents()
        {
            foreach (string _key in _childListener.Keys)
            {
                string key = _key;
                FireChildChangedEvents(key, _childListener[key]);
            }
            foreach (string _key in _dataListener.Keys)
            {
                string key = _key;
                FireChildChangedEvents(key, _childListener[key]);
            }
        }

        public List<string> GetChildren(string path)
        {
            return GetChildren(path, HasListeners(path));
        }

        protected List<string> GetChildren(string path, bool watch)
        {
            return RetryUntilConnected(() =>
            {
                return _connection.GetChildren(path, watch);
            });
        }

        public int CountChildren(string path)
        {
            try
            {
                return GetChildren(path).Count;
            }
            catch (ZkNoNodeException e)
            {
                return 0;
            }
        }

        public bool Exists(string path, bool watch)
        {
            return RetryUntilConnected(() =>
            {
                return _connection.Exists(path, watch);
            });
        }

        public bool Exists(string path)
        {
            return Exists(path, HasListeners(path));
        }

        private void ProcessStateChanged(WatchedEvent @event)
        {
            LOG.Info("zookeeper state changed (" + @event.State + ")");
            SetCurrentState(@event.State);
            if (_shutdownTriggered)
            {
                return;
            }
            FireStateChangedEvent(@event.State);
            if (@event.State == KeeperState.Expired)
            {
                try
                {
                    ReConnect();
                    FireNewSessionEvents();
                }
                catch (Exception e)
                {
                    LOG.Info("Unable to re-establish connection. Notifying consumer of the following exception: ", e);
                    FireSessionEstablishmentError(e);
                }
            }
        }

        private void FireNewSessionEvents()
        {
            foreach (IZkStateListener stateListener in _stateListener)
            {
                //_eventThread.Send(new ZkEventThread.ZkEvent("New session event sent to " + stateListener)
                _eventTask.Send(new ZkTask.ZkEvent("New session event sent to " + stateListener)
                {
                    Run = () =>
                     {
                         stateListener.HandleNewSession();
                     }
                });
            }
        }

        private void FireStateChangedEvent(KeeperState state)
        {
            foreach (IZkStateListener stateListener in _stateListener)
            {
                //_eventThread.Send(new ZkEventThread.ZkEvent("State changed to " + state + " sent to " + stateListener)
                _eventTask.Send(new ZkTask.ZkEvent("State changed to " + state + " sent to " + stateListener)
                {
                    Run = () =>
                    {
                        stateListener.HandleStateChanged(state);
                    }
                });
            }
        }

        private void FireSessionEstablishmentError(Exception error)
        {
            foreach (IZkStateListener stateListener in _stateListener)
            {
                //_eventThread.Send(new ZkEventThread.ZkEvent("State establishment to " + error.Message + " sent to " + stateListener)
                _eventTask.Send(new ZkTask.ZkEvent("State establishment to " + error.Message + " sent to " + stateListener)
                {
                    Run = () =>
                    {
                        stateListener.HandleSessionEstablishmentError(error);
                    }
                });
            }
        }

        private bool HasListeners(string path)
        {
            ConcurrentHashSet<IZkDataListener> dataListeners = null;
            _dataListener.TryGetValue(path, out dataListeners);
            if (dataListeners != null && dataListeners.Count > 0)
            {
                return true;
            }
            ConcurrentHashSet<IZkChildListener> childListeners = null;
            _childListener.TryGetValue(path, out childListeners);
            if (childListeners != null && childListeners.Count > 0)
            {
                return true;
            }
            return false;
        }

        public bool DeleteRecursive(string path)
        {
            List<string> children;
            try
            {
                children = GetChildren(path, false);
            }
            catch (ZkNoNodeException e)
            {
                return true;
            }

            foreach (string subPath in children)
            {
                if (!DeleteRecursive(path + "/" + subPath))
                {
                    return false;
                }
            }
            return Delete(path);
        }

        private void ProcessDataOrChildChange(WatchedEvent @event)
        {
            string path = @event.Path;

            if (@event.Type == EventType.NodeChildrenChanged || @event.Type == EventType.NodeCreated || @event.Type == EventType.NodeDeleted)
            {
                ConcurrentHashSet<IZkChildListener> childListeners  ;
                _childListener.TryGetValue(path, out childListeners);
                if (childListeners != null && !childListeners.IsEmpty())
                {
                    FireChildChangedEvents(path, childListeners);
                }
            }

            if (@event.Type == EventType.NodeDataChanged || @event.Type == EventType.NodeDeleted || @event.Type == EventType.NodeCreated)
            {
                ConcurrentHashSet<IZkDataListener> listeners;
                _dataListener.TryGetValue(path, out listeners);
                if (listeners != null && !listeners.IsEmpty())
                {
                    FireDataChangedEvents(@event.Path, listeners);
                }
            }
        }

        private void FireDataChangedEvents(string path, ConcurrentHashSet<IZkDataListener> listeners)
        {
            foreach (IZkDataListener listener in listeners)
            {
                // _eventThread.Send(new ZkEventThread.ZkEvent("Data of " + path + " changed sent to " + listener)
                _eventTask.Send(new ZkTask.ZkEvent("Data of " + path + " changed sent to " + listener)
                {
                    Run = () =>
                    {
                        // reinstall watch
                        Exists(path, true);
                        try
                        {
                            object data = ReadData<object>(path, null, true);
                            listener.HandleDataChange(path, data);
                        }
                        catch (ZkNoNodeException e)
                        {
                            listener.HandleDataDeleted(path);
                        }
                    }
                });
            }
        }

        private void FireChildChangedEvents(string path, ConcurrentHashSet<IZkChildListener> childListeners)
        {
            try
            {
                // reinstall the watch
                foreach (IZkChildListener listener in childListeners)
                {
                    //_eventThread.Send(new ZkEventThread.ZkEvent("Children of " + path + " changed sent to " + listener)
                    _eventTask.Send(new ZkTask.ZkEvent("Children of " + path + " changed sent to " + listener)
                    {
                        Run = () =>
                        {
                            try
                            {
                                // if the node doesn't exist we should listen for the root node to reappear
                                Exists(path);
                                List<string> children = GetChildren(path);
                                listener.HandleChildChange(path, children);
                            }
                            catch (ZkNoNodeException e)
                            {
                                listener.HandleChildChange(path, null);
                            }
                        }
                    });
                }
            }
            catch (Exception e)
            {
                LOG.Error("Failed to fire child changed event. Unable to getChildren.  ", e);
            }
        }

        public bool Delete(string path)
        {
            return Delete(path, -1);
        }

        public bool Delete(string path, int version)
        {
            try
            {
                RetryUntilConnected(() =>
                {
                    _connection.Delete(path, version);
                    return false;
                });
                return true;
            }
            catch (ZkNoNodeException e)
            {
                return false;
            }
        }

        private byte[] Serialize(object data)
        {
            return _zkSerializer.Serialize(data);
        }

        private T Derializable<T>(byte[] data)
        {
            if (data == null)
            {
                return default(T);
            }
            return (T)_zkSerializer.Deserialize(data);
        }

        public T ReadData<T>(string path)
        {
            return ReadData<T>(path, false);
        }

        private T ReadData<T>(string path, bool returnNullIfPathNotExists)
        {
            T data = default(T);
            try
            {
                data = ReadData<T>(path, null);
            }
            catch (ZkNoNodeException e)
            {
                if (!returnNullIfPathNotExists)
                {
                    throw e;
                }
            }
            return data;
        }

        public T ReadData<T>(string path, Stat stat)
        {
            return ReadData<T>(path, stat, HasListeners(path));
        }

        protected T ReadData<T>(string path, Stat stat, bool watch)
        {
            byte[] data = RetryUntilConnected(() =>
            {
                return _connection.ReadData(path, stat, watch);
            });
            return Derializable<T>(data);
        }

        public void WriteData<T>(string path, T data)
        {
            WriteData(path, data, -1);
        }

        public void UpdateDataSerialized<T>(string path, IDataUpdater<T> updater)
        {
            Stat stat = new Stat();
            bool retry;
            do
            {
                retry = false;
                try
                {
                    T oldData = ReadData<T>(path, stat);
                    T newData = updater.Update(oldData);
                    WriteData(path, newData, stat.Version);
                }
                catch (ZkBadVersionException e)
                {
                    retry = true;
                }
            } while (retry);
        }

        private void WriteData<T>(string path, T data, int expectedVersion)
        {
            WriteDataReturnStat<T>(path, data, expectedVersion);
        }

        public Stat WriteDataReturnStat<T>(string path, T datat, int expectedVersion)
        {
            byte[] data = Serialize(datat);
            return RetryUntilConnected(() =>
            {
                Stat stat = _connection.WriteDataReturnStat(path, data, expectedVersion);
                return stat;
            });
        }

        private void WatchForData(string path)
        {
            RetryUntilConnected(() =>
           {
               return _connection.Exists(path, true);
           });
        }

        /// <summary>
        /// Installs a child watch for the given path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> WatchForChilds(string path)
        {
            if (_zookeeperEventThread != null && Thread.CurrentThread == _zookeeperEventThread)
            {
                throw new ArgumentException("Must not be done in the zookeeper event thread.");
            }
            return RetryUntilConnected(() =>
            {
                Exists(path, true);
                try
                {
                    return GetChildren(path, true);
                }
                catch (ZkNoNodeException e)
                {
                    // ignore, the "exists" watch will listen for the parent node to appear
                }
                return null;
            });
        }

        /// <summary>
        /// Add authentication information to the connection. This will be used to identify the user and check access to
        /// </summary>
        /// <param name="scheme"></param>
        /// <param name="auth"></param>
        public void AddAuthInfo(string scheme, byte[] auth)
        {
            RetryUntilConnected(() =>
           {
               _connection.AddAuthInfo(scheme, auth);
               return false;
           });
        }

        /// <summary>
        /// Connect to ZooKeeper.
        /// </summary>
        /// <param name="connectionTimeout"></param>
        /// <param name="watcher"></param>
        private void Connect(TimeSpan connectionTimeout, IWatcher watcher)
        {
            bool started = false;
            lock (_zkEventLock)
            {
                try
                {
                    _shutdownTriggered = false;
                    //_eventThread = new ZkEventThread(_connection.GetServers());
                    //_eventThread.Start();
                    _eventTask = new ZkTask(_connection.GetServers());
                    _eventTask.Start();
                    _connection.Connect(watcher);

                    LOG.Debug("Awaiting connection to Zookeeper server");
                    bool waitSuccessful = WaitUntilConnected(connectionTimeout);
                    if (!waitSuccessful)
                    {
                        throw new ZkTimeoutException("Unable to connect to zookeeper server within timeout: " + connectionTimeout.Seconds);
                    }
                    started = true;
                }
                finally
                {
                    // we should close the zookeeper instance, otherwise it would keep
                    // on trying to connect
                    if (!started)
                    {
                        Close();
                    }
                }
            }
        }

        public long GetCreationTime(string path)
        {
            lock (_zkEventLock)
            {
                try
                {
                    return _connection.GetCreateTime(path);
                }
                catch (KeeperException e)
                {
                    throw ZkException.Create(e);
                }
                catch (ThreadInterruptedException e)
                {
                    throw new ZkInterruptedException(e);
                }
            }
        }

        public void Close()
        {
            if (_closed)
            {
                return;
            }
            LOG.Debug("Closing ZkClient...");

            lock (_zkEventLock)
            {
                try
                {
                    _shutdownTriggered = true;
                    //_eventThread.Interrupt();
                    //_eventThread.Join(2000);
                    _eventTask.Cancel();
                    _eventTask.Wait(2000);
                    _connection.Close();
                    _closed = true;
                }
                catch (ThreadInterruptedException e)
                {
                    throw new ZkInterruptedException(e);
                }
            }
            LOG.Debug("Closing ZkClient...done");
        }

        private void ReConnect()
        {
            lock (_zkEventLock)
            {
                try
                {
                    _connection.Close();
                    _connection.Connect(this);
                }
                catch (ThreadInterruptedException e)
                {
                    throw new ZkInterruptedException(e);
                }
            }
        }

        public T RetryUntilConnected<T>(Func<T> callable)
        {
            if (_zookeeperEventThread != null && Thread.CurrentThread == _zookeeperEventThread)
            {
                throw new Exception("Must not be done in the zookeeper event thread.");
            }
            long operationStartTime = DateTime.Now.ToUnixTime();
            while (true)
            {
                if (_closed)
                {
                    throw new Exception("ZkClient already closed!");
                }
                try
                {
                    return callable();
                }
                catch (ConnectionLossException e)
                {
                    // we give the event thread some time to update the status to 'Disconnected'
                    Thread.Yield();
                    WaitForRetry();
                }
                catch (SessionExpiredException e)
                {
                    // we give the event thread some time to update the status to 'Expired'
                    Thread.Yield();
                    WaitForRetry();
                }
                catch (KeeperException e)
                {
                    throw ZkException.Create(e);
                }
                catch (ThreadInterruptedException e)
                {
                    throw new ZkInterruptedException(e);
                }
                catch (Exception e)
                {
                    throw ExceptionUtil.ConvertToRuntimeException(e);
                }
                // before attempting a retry, check whether retry timeout has elapsed
                if (_operationRetryTimeoutInMillis > -1 && (DateTime.Now.ToUnixTime() - operationStartTime) >= _operationRetryTimeoutInMillis)
                {
                    throw new ZkTimeoutException("Operation cannot be retried because of retry timeout (" + _operationRetryTimeoutInMillis + " milli seconds)");
                }
            }
        }

        public bool WaitUntilExists(string path, TimeSpan timeOut)
        {
            if (Exists(path))
            {
                return true;
            }

            try
            {
                while (!Exists(path, true))
                {
                    bool gotSignal = _zNodeEventCondition.WaitOne(timeOut);
                    if (!gotSignal)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (ThreadInterruptedException e)
            {
                throw new ZkInterruptedException(e);
            }
        }

        public void WaitUntilConnected()
        {
            WaitUntilConnected(new TimeSpan(0, 0, 0, 0, int.MaxValue));
        }

        public bool WaitUntilConnected(TimeSpan timeOut)
        {
            return WaitForKeeperState(KeeperState.SyncConnected, timeOut);
        }

        public bool WaitForKeeperState(KeeperState keeperState, TimeSpan timeOut)
        {
            if (_zookeeperEventThread != null && Thread.CurrentThread == _zookeeperEventThread)
            {
                throw new Exception("Must not be done in the zookeeper event thread.");
            }

            LOG.Info("Waiting for keeper state " + keeperState);
            try
            {
                bool stillWaiting = true;
                while (_currentState != keeperState)
                {
                    if (!stillWaiting)
                    {
                        return false;
                    }
                    stillWaiting = _stateChangedCondition.WaitOne(timeOut);
                    // Throw an exception in the case authorization fails
                }
                LOG.Debug("State is " + _currentState);
                return true;
            }
            catch (ThreadInterruptedException e)
            {
                throw new ZkInterruptedException(e);
            }
            finally
            {

            }
        }

        private void WaitForRetry()
        {
            if (_operationRetryTimeoutInMillis < 0)
            {
                WaitUntilConnected();
                return;
            }
            WaitUntilConnected(new TimeSpan(0, 0, 0, 0, (int)_operationRetryTimeoutInMillis));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetCurrentState(KeeperState state)
        {
            _currentState = state;
        }

        private AutoResetEvent _stateChangedCondition = new AutoResetEvent(false);
        private AutoResetEvent _zNodeEventCondition = new AutoResetEvent(false);
        private AutoResetEvent _dataChangedCondition = new AutoResetEvent(false);

        public void Process(WatchedEvent @event)
        {
            LOG.Debug("Received event: " + @event.Path);
            _zookeeperEventThread = Thread.CurrentThread;

            bool stateChanged = @event.Path == null;
            bool znodeChanged = @event.Path != null;
            bool dataChanged = @event.Type == EventType.NodeDataChanged ||
                                             @event.Type == EventType.NodeDeleted ||
                                             @event.Type == EventType.NodeCreated ||
                                             @event.Type == EventType.NodeChildrenChanged;

            try
            {
                // We might have to install child change event listener if a new node was created
                if (_shutdownTriggered)
                {
                    LOG.Debug("ignoring event '{" + @event.Type + " | " + @event.Path + "}' since shutdown triggered");
                    return;
                }
                if (stateChanged)
                {
                    ProcessStateChanged(@event);
                }
                if (znodeChanged)
                {
                }
                if (dataChanged)
                {
                    ProcessDataOrChildChange(@event);
                }
            }
            finally
            {
                if (stateChanged)
                {
                    _stateChangedCondition.Set();

                    // If the session expired we have to signal all conditions, because watches might have been removed and
                    // there is no guarantee that those
                    // conditions will be signaled at all after an Expired event
                    // TODO PVo write a test for this
                    if (@event.State == KeeperState.Expired)
                    {
                        _zNodeEventCondition.Set();

                        _dataChangedCondition.Set();

                        // We also have to notify all listeners that something might have changed
                        FireAllEvents();
                    }                                   
                }
                if (znodeChanged)
                {
                    _zNodeEventCondition.Set();
                }
                if (dataChanged)
                {
                    _dataChangedCondition.Set();
                }
                LOG.Debug("Leaving process event");
            }
        }

        public int NumberOfListeners()
        {
            int listeners = 0;
            foreach (ConcurrentHashSet<IZkChildListener> childListeners in _childListener.Values)
            {
                listeners += childListeners.Count;
            }
            foreach (ConcurrentHashSet<IZkDataListener> dataListeners in _dataListener.Values)
            {
                listeners += dataListeners.Count;
            }
            listeners += _stateListener.Count;

            return listeners;
        }

    }
}
