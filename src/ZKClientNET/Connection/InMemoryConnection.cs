using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;
using System.Collections.Concurrent;
using System.Threading;
using static ZooKeeperNet.KeeperException;
using ZKClientNET.Exceptions;
using ZKClientNET.Util;

namespace ZKClientNET.Connection
{
    public class InMemoryConnection : IZKConnection
    {
        public class DataAndVersion
        {
            public byte[] data { set; get; }
            public int version { set; get; }
            public List<ACL> acl { set; get; }

            public DataAndVersion() { }

            public DataAndVersion(byte[] data, int version, List<ACL> acl)
            {
                this.data = data;
                this.version = version;
                this.acl = acl;
            }

            public DataAndVersion(byte[] data, int version) : this(data, version, null) { }
        }

        private object _lock = new object();
        private Dictionary<string, DataAndVersion> _data = new Dictionary<string, DataAndVersion>();
        private Dictionary<string, long?> _creationTime = new Dictionary<string, long?>();
        private List<ZKId> _ids = new List<ZKId>();
        private int sequence = 0;

        private HashSet<string> _dataWatches = new HashSet<string>();
        private HashSet<string> _nodeWatches = new HashSet<string>();
        private EventTask _eventTask;

        private class EventTask
        {
            private IWatcher _watcher;
            private Task _eventTask;
            private BlockingCollection<WatchedEvent> _blockingQueue = new BlockingCollection<WatchedEvent>(new ConcurrentQueue<WatchedEvent>());
            private CancellationTokenSource tokenSource = new CancellationTokenSource();

            public EventTask(IWatcher watcher)
            {
                _watcher = watcher;
                _eventTask = new Task(Run, tokenSource.Token);
            }

            public void ReSet()
            {
                tokenSource = new CancellationTokenSource();
                _eventTask = new Task(Run, tokenSource.Token);
            }

            public void Start()
            {
                _eventTask.Start();
            }

            private void Run()
            {
                try
                {
                    while (!tokenSource.IsCancellationRequested)
                    {
                        _watcher.Process(_blockingQueue.Take());
                    }
                }
                catch
                {
                    // stop event thread
                }
            }

            public void Cancel()
            {
                tokenSource.Cancel();
            }

            public void Wait()
            {
                _eventTask.Wait();
            }

            public void Send(WatchedEvent @event)
            {
                if (_eventTask != null && _eventTask.Status != TaskStatus.Canceled)
                {
                    _blockingQueue.Add(@event);
                }
            }
        }

        public InMemoryConnection()
        {
            try
            {
                Create("/", null, CreateMode.Persistent);
            }
            catch (KeeperException e)
            {
                throw ZKException.Create(e);
            }
            catch (ThreadInterruptedException e)
            {
                Thread.CurrentThread.Interrupt();
                throw new ZKInterruptedException(e);
            }
        }

        public void Connect(IWatcher watcher)
        {
            lock (_lock)
            {
                if (_eventTask != null)
                {
                    throw new Exception("Already connected.");
                }
                _eventTask = new EventTask(watcher);
                _eventTask.Start();
                _eventTask.Send(new WatchedEvent(KeeperState.SyncConnected, EventType.None, null));
            }
        }

        public void ReConnect(IWatcher watcher)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            try
            {
                lock (_lock)
                {
                    if (_eventTask != null)
                    {
                        _eventTask.Cancel();
                        _eventTask.Wait();
                        _eventTask = null;
                    }
                }
            }
            finally
            {
            }
        }

        public string Create(string path, byte[] data, List<ACL> acl, CreateMode mode)
        {
            lock (_lock)
            {
                if (mode.Sequential)
                {
                    Interlocked.Increment(ref sequence);
                    int newSequence = sequence;
                    path = path + ZKPathUtil.LeadingZeros(newSequence, 10);
                }

                if (Exists(path, false))
                {
                    throw new KeeperException.NodeExistsException();
                }
                string parentPath = GetParentPath(path);
                CheckACL(parentPath, Perms.CREATE);
                _data.Add(path, new DataAndVersion(data, 0, acl));
                _creationTime.Add(path, DateTime.Now.ToUnixTime());
                CheckWatch(_nodeWatches, path, EventType.NodeCreated);
                // we also need to send a child change event for the parent
                if (!string.IsNullOrEmpty(parentPath))
                {
                    CheckWatch(_nodeWatches, parentPath, EventType.NodeChildrenChanged);
                }
                return path;
            }
        }

        public string Create(string path, byte[] data, CreateMode mode)
        {
            return Create(path, data, null, mode);
        }

        private string GetParentPath(string path)
        {
            int lastIndexOf = path.LastIndexOf("/");
            if (lastIndexOf == -1 || lastIndexOf == 0)
            {
                return string.Empty;
            }
            return path.Substring(0, lastIndexOf);
        }

        public void Delete(string path)
        {
            Delete(path, -1);
        }

        public void Delete(string path, int version)
        {
            lock (_lock)
            {
                if (!Exists(path, false))
                {
                    throw new KeeperException.NoNodeException();
                }
                string parentPath = GetParentPath(path);
                CheckACL(parentPath, Perms.DELETE);
                // If version isn't -1, check that it mateches
                if (version != -1)
                {
                    DataAndVersion item;
                    _data.TryGetValue(path, out item);
                    if (item.version != version)
                    {
                        throw KeeperException.Create(Code.BADVERSION);
                    }
                }
                _data.Remove(path);
                _creationTime.Remove(path);
                CheckWatch(_nodeWatches, path, EventType.NodeDeleted);
                if (parentPath != null)
                {
                    CheckWatch(_nodeWatches, parentPath, EventType.NodeChildrenChanged);
                }
            }
        }

        public bool Exists(string path, bool watch)
        {
            lock (_lock)
            {
                if (watch)
                {
                    InstallWatch(_nodeWatches, path);
                }
                return _data.ContainsKey(path);
            }
        }

        private void InstallWatch(HashSet<string> watches, string path)
        {
            watches.Add(path);
        }

        public List<string> GetChildren(string path, bool watch)
        {
            if (!Exists(path, false))
            {
                throw KeeperException.Create(Code.NONODE, path);
            }
            if (Exists(path, false) && watch)
            {
                InstallWatch(_nodeWatches, path);
            }

            CheckACL(path, Perms.READ);
            List<string> children = new List<string>();
            string[] directoryStack = path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string str in _data.Keys)
            {
                if (str.StartsWith(path))
                {
                    string[] stack = str.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
                    // is one folder level below the one we loockig for and starts
                    // with path...
                    if (stack.Length == directoryStack.Length + 1)
                    {
                        children.Add(stack[stack.Length - 1]);
                    }
                }

            }
            return children;
        }

        public ZooKeeper.States GetZookeeperState()
        {
            lock (_lock)
            {
                if (_eventTask == null)
                {
                    return ZooKeeper.States.CLOSED;
                }
                return ZooKeeper.States.CONNECTED;
            }
        }

        public byte[] ReadData(string path, Stat stat, bool watch)
        {
            if (watch)
            {
                InstallWatch(_dataWatches, path);
            }

            lock (_lock)
            {
                DataAndVersion dataAndVersion;
                _data.TryGetValue(path, out dataAndVersion);
                if (dataAndVersion == null)
                {
                    throw new ZKNoNodeException(new KeeperException.NoNodeException());
                }
                CheckACL(path, Perms.READ);
                byte[] bs = dataAndVersion.data;
                if (stat != null)
                    stat.Version = dataAndVersion.version;
                return bs;
            }
        }

        public void WriteData(string path, byte[] data, int expectedVersion)
        {
            WriteDataReturnStat(path, data, expectedVersion);
        }

        public Stat WriteDataReturnStat(string path, byte[] data, int expectedVersion)
        {
            int newVersion = -1;
            lock (_lock)
            {
                CheckWatch(_dataWatches, path, EventType.NodeDataChanged);
                if (!Exists(path, false))
                {
                    throw new KeeperException.NoNodeException();
                }
                CheckACL(path, Perms.WRITE);
                newVersion = _data[path].version + 1;
                _data[path] = new DataAndVersion(data, newVersion);
                string parentPath = GetParentPath(path);
                if (!string.IsNullOrEmpty(parentPath))
                {
                    CheckWatch(_nodeWatches, parentPath, EventType.NodeChildrenChanged);
                }
            }
            Stat stat = new Stat();
            stat.Version = newVersion;
            return stat;
        }

        private void CheckWatch(HashSet<string> watches, string path, EventType eventType)
        {
            if (watches.Contains(path))
            {
                watches.Remove(path);
                _eventTask.Send(new WatchedEvent(KeeperState.SyncConnected, eventType, path));
            }
        }

        public long GetCreateTime(string path)
        {
            long? time = _creationTime[path];
            if (!time.HasValue)
            {
                return -1;
            }
            return time.Value;
        }

        public string GetServers()
        {
            return "mem";
        }

        public void AddAuthInfo(string scheme, byte[] auth)
        {
            _ids.Add(new ZKId(scheme, Encoding.Default.GetString(auth)));
        }

        public void SetACL(string path, List<ACL> acl, int version)
        {
            if (!Exists(path, false))
            {
                throw new KeeperException.NoNodeException();
            }

            DataAndVersion dataAndVersion = _data[path];
            if (version != dataAndVersion.version)
            {
                throw new KeeperException.BadVersionException();
            }

            CheckACL(path, Perms.ADMIN);

            lock (_lock)
            {
                _data.Add(path, new DataAndVersion(dataAndVersion.data, dataAndVersion.version + 1, acl));
            }
        }

        public KeyValuePair<List<ACL>, Stat> GetACL(string path)
        {
            if (!Exists(path, false))
            {
                throw new KeeperException.NoNodeException();
            }

            DataAndVersion dataAndVersion = _data[path];

            Stat stat = new Stat();
            stat.Version = dataAndVersion.version;
            stat.Ctime = _creationTime[path].Value;
            return new KeyValuePair<List<ACL>, Stat>(dataAndVersion.acl, stat) ;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">path of znode we are accessing</param>
        /// <param name="perm">Privileges required for the action</param>
        private void CheckACL(string path, int perm)
        {
            DataAndVersion node;
            _data.TryGetValue(path, out node);
            if (node == null)
            {
                return;
            }
            List<ACL> acl = node.acl;
            if (acl == null || acl.Count == 0)
            {
                return;
            }
            foreach (ZKId authId in _ids)
            {
                if (authId.Scheme == "super")
                {
                    return;
                }
            }
            foreach (ACL a in acl)
            {
                ZKId id = a.Id;
                if ((a.Perms & perm) != 0)
                {
                    if (id.Scheme.ToLower() == "world" && id.Id.ToLower() == "anyone")
                    {
                        return;
                    }
                    foreach (ZKId authId in _ids)
                    {
                        if (authId.Scheme == id.Scheme && authId.Id == id.Id)
                        {
                            return;
                        }
                    }
                }
            }
            throw new KeeperException.NoAuthException();
        }
       
    }
}
