using Org.Apache.Zookeeper.Data;
using System.Collections.Generic;
using ZooKeeperNet;

namespace ZKClientNET.Connection
{
    public interface IZKConnection
    {
        void Connect(IWatcher watcher);

        void ReConnect(IWatcher watcher);

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

        void AddAuthInfo(string scheme, byte[] auth);

        void SetACL(string path, List<ACL> acl, int version);

        KeyValuePair<List<ACL>, Stat> GetACL(string path);
    }
}
