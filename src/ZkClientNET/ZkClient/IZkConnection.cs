using Org.Apache.Zookeeper.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZooKeeperNet;

namespace ZkClientNET.ZkClient
{
    public interface IZkConnection
    {
        void Connect(IWatcher watcher);

        void Close();

        string Create(string path, byte[] data, CreateMode mode);

        string Create(string path, byte[] data, List<ACL> acl, CreateMode mode);

        void Delete(string path);

        void Delete(string path, int version);

        bool Exists(string path, bool watch);

        List<string> GetChildren(string path, bool watch);

        byte[] ReadData(string path, Stat stat, bool watch);

        void WriteData(string path, byte[] data, int expectedVersion);

        Stat WriteDataReturnStat(string path, byte[] data, int expectedVersion);

        ZooKeeper.States GetZookeeperState();

        long GetCreateTime(string path);

        string GetServers();

        // List<OpResult> multi(Iterable<Op> ops) throws KeeperException, InterruptedException;

        void AddAuthInfo(string scheme, byte[] auth);

        void SetAcl(string path, List<ACL> acl, int version);

        Dictionary<List<ACL>, Stat> GetAcl(string path);
    }
}
