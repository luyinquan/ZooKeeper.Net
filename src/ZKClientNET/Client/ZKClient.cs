using log4net;
using Org.Apache.Zookeeper.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using ZKClientNET.Connection;
using ZKClientNET.Exceptions;
using ZKClientNET.Listener;
using ZKClientNET.Serialize;
using ZKClientNET.Util;
using ZooKeeperNet;
using static ZooKeeperNet.KeeperException;

namespace ZKClientNET.Client
{
    /// <summary>
    /// ZooKeeper客户端
    /// 断线重连，会话过期处理，永久监听，子节点数据变化监听
    /// </summary>
    public class ZKClient : IWatcher
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZKClient));
               
        private ConcurrentDictionary<string, ConcurrentHashSet<IZKChildListener>> _childListener = new ConcurrentDictionary<string, ConcurrentHashSet<IZKChildListener>>();
        private ConcurrentDictionary<string, ConcurrentHashSet<IZKDataListener>> _dataListener = new ConcurrentDictionary<string, ConcurrentHashSet<IZKDataListener>>();
        private ConcurrentHashSet<IZKStateListener> _stateListener = new ConcurrentHashSet<IZKStateListener>();

        private object _zkEventLock = new object();
        private Thread _zookeeperEventThread;
        protected long _operationRetryTimeoutInMillis;
        protected IZKConnection _connection;
        private KeeperState _currentState;
        private IZKSerializer _zkSerializer;
        /// <summary>
        /// 触发关闭的标记，如果为true证明正在关闭客户端及连接   
        /// </summary>
        private bool _shutdownTriggered;
        /// <summary>
        /// 是否已关闭客户端的标记
        /// </summary>
        private volatile bool _closed;
        private ZKTask _eventTask;

        #region ZKClient
        public ZKClient(string serverstring) : this(serverstring, new TimeSpan(0, 0, 0, 0, int.MaxValue))
        {

        }

        public ZKClient(string zkServers, TimeSpan connectionTimeout) : this(new ZKConnection(zkServers), connectionTimeout)
        {

        }

        public ZKClient(string zkServers, TimeSpan sessionTimeout, TimeSpan connectionTimeout) : this(new ZKConnection(zkServers, sessionTimeout), connectionTimeout)
        {

        }

        public ZKClient(string zkServers, TimeSpan sessionTimeout, TimeSpan connectionTimeout, IZKSerializer zkSerializer) : this(new ZKConnection(zkServers, sessionTimeout), connectionTimeout, zkSerializer)
        {

        }

        /// <summary>
        /// Most operations done through this {@link org.I0Itec.zkclient.ZKClient}
        /// are retried in cases like
        /// connection loss with the Zookeeper servers.During such failures, this
        /// <code>operationRetryTimeout</code> decides the maximum amount of time, in milli seconds, each
        ///operation is retried.A value lesser than 0 is considered as
        ///"retry forever until a connection has been reestablished".
        /// </summary>
        /// <param name="zkServers">  The Zookeeper servers</param>
        /// <param name="sessionTimeout">The session timeout in milli seconds</param>
        /// <param name="connectionTimeout">The connection timeout in milli seconds</param>
        /// <param name="IZKSerializer"> The Zookeeper data serializer</param>
        /// <param name="operationRetryTimeout"> operationRetryTimeout</param>

        public ZKClient(string zkServers, TimeSpan sessionTimeout, TimeSpan connectionTimeout, IZKSerializer zkSerializer, long operationRetryTimeout) : this(new ZKConnection(zkServers, sessionTimeout), connectionTimeout, zkSerializer, operationRetryTimeout)
        {

        }

        public ZKClient(IZKConnection connection) : this(connection, new TimeSpan(0, 0, 0, 0, int.MaxValue))
        {

        }

        public ZKClient(IZKConnection connection, TimeSpan connectionTimeout) : this(connection, connectionTimeout, new SerializableSerializer())
        {

        }

        public ZKClient(IZKConnection zkConnection, TimeSpan connectionTimeout, IZKSerializer zkSerializer) : this(zkConnection, connectionTimeout, zkSerializer, -1)
        {

        }

        /// <summary>
        /// Most operations done through this {@link org.I0Itec.zkclient.ZKClient} are retried in cases like
        /// connection loss with the Zookeeper servers.During such failures, this
        /// <code>operationRetryTimeout</code> decides the maximum amount of time, in milli seconds, each
        /// operation is retried.A value lesser than 0 is considered as
        /// retry forever until a connection has been reestablished".
        /// </summary>
        /// <param name="zkConnection"> The Zookeeper servers</param>
        /// <param name="connectionTimeout"> The connection timeout in milli seconds</param>
        /// <param name="IZKSerializer"></param>
        /// <param name="operationRetryTimeout"> The Zookeeper data serializer</param>
        public ZKClient(IZKConnection zkConnection, TimeSpan connectionTimeout, IZKSerializer zkSerializer, long operationRetryTimeout)
        {
            if (zkConnection == null)
            {
                throw new ArgumentNullException("Zookeeper connection is null!");
            }
            _connection = zkConnection;
            _zkSerializer = zkSerializer;
            _operationRetryTimeoutInMillis = operationRetryTimeout;
            //_isZKSaslEnabled = IsZKSaslEnabled();
            Connect(connectionTimeout, this);
        }
        #endregion

        public void SetZKSerializer(IZKSerializer zkSerializer)
        {
            _zkSerializer = zkSerializer;
        }

        public List<string> SubscribeChildChanges(string path, IZKChildListener listener)
        {
            //Monitor.Enter(_childListener);
            ConcurrentHashSet<IZKChildListener> listeners;
            _childListener.TryGetValue(path, out listeners);
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZKChildListener>();
                _childListener.TryAdd(path, listeners);
            }
            listeners.Add(listener);
            //Monitor.Exit(_childListener);
            return WatchForChilds(path);
        }

        public void UnSubscribeChildChanges(string path, IZKChildListener childListener)
        {
            //Monitor.Enter(_childListener);
            ConcurrentHashSet<IZKChildListener> listeners;
            _childListener.TryGetValue(path, out listeners);
            if (listeners != null)
            {
                listeners.Remove(childListener);
            }
            //Monitor.Exit(_childListener);
        }

        public void SubscribeDataChanges(string path, IZKDataListener listener)
        {
            ConcurrentHashSet<IZKDataListener> listeners;
            //Monitor.Enter(_dataListener);
            _dataListener.TryGetValue(path, out listeners);
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZKDataListener>();
                _dataListener.TryAdd(path, listeners);
            }
            listeners.Add(listener);
            //Monitor.Exit(_dataListener);
            WatchForData(path);
            LOG.Debug("Subscribed data changes for " + path);
        }

        public void UnSubscribeDataChanges(string path, IZKDataListener dataListener)
        {
            //Monitor.Enter(_dataListener);
            ConcurrentHashSet<IZKDataListener> listeners;
            _dataListener.TryGetValue(path, out listeners);
            if (listeners != null)
            {
                listeners.Remove(dataListener);
            }
            if (listeners == null || listeners.IsEmpty)
            {
                ConcurrentHashSet<IZKDataListener> _listeners;
                _dataListener.TryRemove(path, out _listeners);
            }
            //Monitor.Exit(_dataListener);
        }

        public void SubscribeStateChanges(IZKStateListener listener)
        {
            //Monitor.Enter(_stateListener);
            _stateListener.Add(listener);
            //Monitor.Exit(_stateListener);
        }

        public void UnSubscribeStateChanges(IZKStateListener stateListener)
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
        /// 递归创建节点,如果节点的上层节点不存在，则自动创建
        ///上层节点都被创建为PERSISTENT类型的节点
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="createMode"></param>
        public void CreateRecursive(string path, object data, CreateMode createMode)
        {
            CreateRecursive(path, data, Ids.OPEN_ACL_UNSAFE, createMode);
        }

        /// <summary>
        /// 递归创建节点,如果节点的上层节点不存在，则自动创建
        ///上层节点都被创建为PERSISTENT类型的节点
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="acl"></param>
        /// <param name="createMode"></param>
        public void CreateRecursive(string path, object data, List<ACL> acl, CreateMode createMode)
        {
            try
            {
                Create(path, data, acl, createMode);
            }
            catch (ZKNodeExistsException)
            {
                LOG.Error(path + " not exists");
            }
            catch (ZKNoNodeException e)
            {
                string parentDir = path.Substring(0, path.LastIndexOf('/'));
                CreateRecursive(parentDir, null, acl, CreateMode.Persistent);
                Create(path, data, acl, createMode);
            }
        }

        public void CreatePersistent(string path)
        {
            CreatePersistent(path, false);
        }

        private void CreatePersistent(string path, bool createParents)
        {
            CreatePersistent(path, createParents, Ids.OPEN_ACL_UNSAFE);
        }

        private void CreatePersistent(string path, bool createParents, List<ACL> acl)
        {
            try
            {
                Create(path, null, acl, CreateMode.Persistent);
            }
            catch (ZKNodeExistsException e)
            {
                if (!createParents)
                {
                    throw e;
                }
            }
            catch (ZKNoNodeException e)
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
        /// 设置访问权限
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

        public void CreatePersistent(string path, object data)
        {
            Create(path, data, CreateMode.Persistent);
        }

        public void CreatePersistent(string path, object data, List<ACL> acl)
        {
            Create(path, data, acl, CreateMode.Persistent);
        }

        public string CreatePersistentSequential(string path, object data)
        {
            return Create(path, data, CreateMode.PersistentSequential);
        }

        public string CreatePersistentSequential(string path, object data, List<ACL> acl)
        {
            return Create(path, data, acl, CreateMode.PersistentSequential);
        }

        /// <summary>
        /// 会话失效后临时节点会被删除，并且在重新连接后并不会重新创建。
        /// </summary>
        /// <param name="path"></param>
        public void CreateEphemeral(string path)
        {
            Create(path, null, CreateMode.Ephemeral);
        }

        public void CreateEphemeral(string path, List<ACL> acl)
        {
            Create(path, null, acl, CreateMode.Ephemeral);
        }

        public string Create(string path, object data, CreateMode mode)
        {
            return Create(path, data, Ids.OPEN_ACL_UNSAFE, mode);
        }

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

        public void CreateEphemeral(string path, object data)
        {
            Create(path, data, CreateMode.Ephemeral);
        }

        public string CreateEphemeralSequential(string path, object data)
        {
            return Create(path, data, CreateMode.EphemeralSequential);
        }

        public string CreateEphemeralSequential(string path, object data, List<ACL> acl)
        {
            return Create(path, data, acl, CreateMode.EphemeralSequential);
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
            catch (ZKNoNodeException)
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

        public bool DeleteRecursive(string path)
        {
            List<string> children;
            try
            {
                children = GetChildren(path, false);
            }
            catch (ZKNoNodeException e)
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
            catch (ZKNoNodeException e)
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
            catch (ZKNoNodeException e)
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
                catch (ZKBadVersionException e)
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

        /// <summary>
        /// 监听数据变化
        /// </summary>
        /// <param name="path"></param>
        private void WatchForData(string path)
        {
            RetryUntilConnected(() =>
           {
               return _connection.Exists(path, true);
           });
        }

        /// <summary>
        /// 监听子节点变化
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
                catch (ZKNoNodeException e)
                {
                    // ignore, the "exists" watch will listen for the parent node to appear
                }
                return null;
            });
        }

        /// <summary>
        /// 添加认证信息，用于访问被ACL保护的节点
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

        private void Connect(TimeSpan connectionTimeout, IWatcher watcher)
        {
            bool started = false;
            lock (_zkEventLock)
            {
                try
                {
                    _shutdownTriggered = false;
                    _eventTask = new ZKTask(_connection.GetServers());
                    _eventTask.Start();
                    _connection.Connect(watcher);

                    LOG.Debug("Awaiting connection to Zookeeper server");
                    bool waitSuccessful = WaitUntilConnected(connectionTimeout);
                    if (!waitSuccessful)
                    {
                        throw new ZKTimeoutException("Unable to connect to zookeeper server within timeout: " + connectionTimeout.Seconds);
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
                    throw ZKException.Create(e);
                }
                catch (ThreadInterruptedException e)
                {
                    throw new ZKInterruptedException(e);
                }
            }
        }

        public void Close()
        {
            if (_closed)
            {
                return;
            }
            LOG.Debug("Closing ZKClient...");

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
                    throw new ZKInterruptedException(e);
                }
            }
            LOG.Debug("Closing ZKClient...done");
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
                    throw new ZKInterruptedException(e);
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
                    throw new Exception("ZKClient already closed!");
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
                    throw ZKException.Create(e);
                }
                catch (ThreadInterruptedException e)
                {
                    throw new ZKInterruptedException(e);
                }
                catch (Exception e)
                {
                    throw ExceptionUtil.ConvertToRuntimeException(e);
                }
                // before attempting a retry, check whether retry timeout has elapsed
                if (_operationRetryTimeoutInMillis > -1 && 
                    (DateTime.Now.ToUnixTime() - operationStartTime) >= _operationRetryTimeoutInMillis)
                {
                    throw new ZKTimeoutException("Operation cannot be retried because of retry timeout (" + _operationRetryTimeoutInMillis + " milli seconds)");
                }
            }
        }

        /// <summary>
        /// 等待直到数据被创建
        /// </summary>
        /// <param name="path"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
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
                throw new ZKInterruptedException(e);
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
                throw new ZKInterruptedException(e);
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

        public KeeperState GetCurrentState()
        {
            return _currentState;
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
                        FireAllEvents(@event.Type);
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

        private void FireAllEvents(EventType eventType)
        {
            foreach (string _key in _childListener.Keys)
            {
                string key = _key;
                FireChildChangedEvents(key, _childListener[key], eventType);
            }
            foreach (string _key in _dataListener.Keys)
            {
                string key = _key;
                FireChildChangedEvents(key, _childListener[key], eventType);
            }
        }

        private void ProcessDataOrChildChange(WatchedEvent @event)
        {
            string path = @event.Path;

            if (@event.Type == EventType.NodeChildrenChanged || @event.Type == EventType.NodeCreated || @event.Type == EventType.NodeDeleted)
            {
                ConcurrentHashSet<IZKChildListener> childListeners;
                _childListener.TryGetValue(path, out childListeners);
                if (childListeners != null && !childListeners.IsEmpty())
                {
                    FireChildChangedEvents(path, childListeners, @event.Type);
                }
            }

            if (@event.Type == EventType.NodeDataChanged || @event.Type == EventType.NodeDeleted || @event.Type == EventType.NodeCreated)
            {
                ConcurrentHashSet<IZKDataListener> listeners;
                _dataListener.TryGetValue(path, out listeners);
                if (listeners != null && !listeners.IsEmpty())
                {
                    FireDataChangedEvents(@event.Path, listeners, @event.Type);
                }
            }
        }

        private void FireChildChangedEvents(string path, ConcurrentHashSet<IZKChildListener> childListeners, EventType eventType)
        {
            try
            {
                // reinstall the watch
                foreach (IZKChildListener listener in childListeners)
                {
                    _eventTask.Send(new ZKTask.ZKEvent("Children of " + path + " changed sent to " + nameof(listener))
                    {
                        Run = () =>
                        {
                            try
                            {
                                // if the node doesn't exist we should listen for the root node to reappear
                                Exists(path);
                                List<string> children = GetChildren(path);
                                listener.HandleChildChange(path, children);
                                //子节点个数变化
                                if (eventType == EventType.NodeChildrenChanged
                                   || eventType == EventType.NodeCreated
                                   || eventType == EventType.NodeDeleted)
                                {
                                    listener.HandleChildCountChanged(path, children);
                                }
                            }
                            catch (ZKNoNodeException e)
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

        private void FireDataChangedEvents(string path, ConcurrentHashSet<IZKDataListener> listeners, EventType eventType)
        {
            foreach (IZKDataListener listener in listeners)
            {
                _eventTask.Send(new ZKTask.ZKEvent("Data of " + path + " changed sent to " + nameof(listener))
                {
                    Run = () =>
                    {
                        // reinstall watch
                        Exists(path, true);
                        try
                        {
                            object data = ReadData<object>(path, null, true);
                            if (eventType == EventType.NodeCreated)
                            {
                                listener.HandleDataCreated(path, data);
                            }
                            else
                            {
                                listener.HandleDataChange(path, data);
                            }

                            listener.HandleDataCreatedOrChange(path, data);
                        }
                        catch (ZKNoNodeException e)
                        {
                            listener.HandleDataDeleted(path);
                        }
                    }
                });
            }
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
            foreach (IZKStateListener stateListener in _stateListener)
            {
                _eventTask.Send(new ZKTask.ZKEvent("New session event sent to " + nameof(stateListener))
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
            foreach (IZKStateListener stateListener in _stateListener)
            {
                //_eventThread.Send(new ZKEventThread.ZKEvent("State changed to " + state + " sent to " + stateListener)
                _eventTask.Send(new ZKTask.ZKEvent("State changed to " + state + " sent to " + nameof(stateListener))
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
            foreach (IZKStateListener stateListener in _stateListener)
            {
                _eventTask.Send(new ZKTask.ZKEvent("State establishment to " + error.Message + " sent to " + nameof(stateListener))
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
            ConcurrentHashSet<IZKDataListener> dataListeners = null;
            _dataListener.TryGetValue(path, out dataListeners);
            if (dataListeners != null && dataListeners.Count > 0)
            {
                return true;
            }
            ConcurrentHashSet<IZKChildListener> childListeners = null;
            _childListener.TryGetValue(path, out childListeners);
            if (childListeners != null && childListeners.Count > 0)
            {
                return true;
            }
            return false;
        }

        public int NumberOfListeners()
        {
            int listeners = 0;
            foreach (ConcurrentHashSet<IZKChildListener> childListeners in _childListener.Values)
            {
                listeners += childListeners.Count;
            }
            foreach (ConcurrentHashSet<IZKDataListener> dataListeners in _dataListener.Values)
            {
                listeners += dataListeners.Count;
            }
            listeners += _stateListener.Count;

            return listeners;
        }

    }
}
