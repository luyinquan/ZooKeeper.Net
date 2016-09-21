using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.Apache.Zookeeper.Data;
using ZooKeeperNet;

namespace ZkClientNET.ZkClient
{
    public class ZkConnection : IZkConnection
    {

        /** It is recommended to use quite large sessions timeouts for ZooKeeper. */
        private static TimeSpan DEFAULT_SESSION_TIMEOUT = new TimeSpan(0, 0, 0, 0, 30000);

        private ZooKeeper _zk = null;
        //private Lock _zookeeperLock = new ReentrantLock();

        private  string _servers;
        private  TimeSpan _sessionTimeOut;

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
            throw new NotImplementedException();
        }

        public void AddAuthInfo(string scheme, byte[] auth)
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }
   
        public string Create(string path, byte[] data, CreateMode mode)
        {
            throw new NotImplementedException();
        }

        public string Create(string path, byte[] data, List<ACL> acl, CreateMode mode)
        {
            throw new NotImplementedException();
        }

        public void Delete(string path)
        {
            throw new NotImplementedException();
        }

        public void Delete(string path, int version)
        {
            throw new NotImplementedException();
        }

        public bool Exists(string path, bool watch)
        {
            throw new NotImplementedException();
        }

        public Dictionary<List<ACL>, Stat> GetAcl(string path)
        {
            throw new NotImplementedException();
        }

        public List<string> GetChildren(string path, bool watch)
        {
            throw new NotImplementedException();
        }

        public long GetCreateTime(string path)
        {
            throw new NotImplementedException();
        }

        public string GetServers()
        {
            throw new NotImplementedException();
        }

        public ZooKeeper.States GetZookeeperState()
        {
            throw new NotImplementedException();
        }

        public byte[] ReadData(string path, Stat stat, bool watch)
        {
            throw new NotImplementedException();
        }

        public void SetAcl(string path, List<ACL> acl, int version)
        {
            throw new NotImplementedException();
        }

        public void WriteData(string path, byte[] data, int expectedVersion)
        {
            throw new NotImplementedException();
        }

        public Stat WriteDataReturnStat(string path, byte[] data, int expectedVersion)
        {
            throw new NotImplementedException();
        }
    }
}
