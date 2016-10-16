using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;
using ZkClientNET.ZkClient.Exceptions;
using log4net;
using System.Threading;

namespace ZkClientNET.ZkClient
{
    public class ZkConnection : IZkConnection
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ZkConnection));

        /** It is recommended to use quite large sessions timeouts for ZooKeeper. */
        private static TimeSpan DEFAULT_SESSION_TIMEOUT = new TimeSpan(0, 0, 0, 30000);

        private ZooKeeper _zk { set; get; } = null;
   
        public string _servers { set; get; }

        private TimeSpan _sessionTimeOut { set; get; }

        private object _zookeeperLock = new object();

        public ZkConnection(string zkServers) : this(zkServers, DEFAULT_SESSION_TIMEOUT)
        {

        }

        public ZkConnection(string zkServers, TimeSpan sessionTimeOut)
        {
            _servers = zkServers;
            _sessionTimeOut = sessionTimeOut;
        }

        public void Connect(IWatcher watcher)
        {
            lock (_zookeeperLock)
            {
                if (_zk != null)
                {
                    throw new Exception("zk client has already been started");
                }
                try
                {
                    LOG.Debug("Creating new ZookKeeper instance to connect to " + _servers + ".");
                    _zk = new ZooKeeper(_servers, _sessionTimeOut, watcher);
                }
                catch (Exception e)
                {
                    throw new ZkException("Unable to connect to " + _servers, e);
                }
            }
        }

        public void Close()
        {
            lock (_zookeeperLock)
            {
                if (_zk != null)
                {
                    try
                    {

                        LOG.Debug("Closing ZooKeeper connected to " + _servers);
                        _zk.Dispose();
                        _zk = null;
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        throw;
                    }
                }
            }
        }

        public string Create(string path, byte[] data, CreateMode mode)
        {
            return _zk.Create(path, data, Ids.OPEN_ACL_UNSAFE, mode);
        }

        public string Create(string path, byte[] data, List<ACL> acl, CreateMode mode)
        {
            return _zk.Create(path, data, acl, mode);
        }

        public void Delete(string path)
        {
            _zk.Delete(path, -1);
        }

        public void Delete(string path, int version)
        {
            _zk.Delete(path, version);
        }

        public bool Exists(string path, bool watch)
        {
            return _zk.Exists(path, watch) != null;
        }

        public List<string> GetChildren(string path, bool watch)
        {
            return _zk.GetChildren(path, watch).ToList();
        }

        public byte[] ReadData(string path, Stat stat, bool watch)
        {
            return _zk.GetData(path, watch, stat);
        }

        public void WriteData(string path, byte[] data)
        {
            WriteData(path, data, -1);
        }

        public void WriteData(string path, byte[] data, int version)
        {
            _zk.SetData(path, data, version);
        }

        public Stat WriteDataReturnStat(string path, byte[] data, int expectedVersion)
        {
            return _zk.SetData(path, data, expectedVersion);
        }

        public ZooKeeper.States GetZookeeperState()
        {
            return _zk != null ? _zk.State : null;
        }

        public long GetCreateTime(string path)
        {
            Stat stat = _zk.Exists(path, false);
            if (stat != null)
            {
                return stat.Ctime;
            }
            return -1;
        }

        public void AddAuthInfo(string scheme, byte[] auth)
        {
            _zk.AddAuthInfo(scheme, auth);
        }

        public void SetACL(string path, List<ACL> acl, int version)
        {
            _zk.SetACL(path, acl, version);
        }

        public KeyValuePair<List<ACL>, Stat> GetACL(string path)
        {
            Stat stat = new Stat();
            List<ACL> acl = _zk.GetACL(path, stat).ToList();
            return new KeyValuePair<List<ACL>, Stat>(acl, stat);
        }

        public string GetServers()
        {
            throw new NotImplementedException();
        }
    }
}
