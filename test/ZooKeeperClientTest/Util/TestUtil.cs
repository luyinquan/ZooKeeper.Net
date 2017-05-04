using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZooKeeperClient.Client;

namespace ZooKeeperClient.Util
{
    public class TestUtil
    {
        //zk集群的地址  
        public static string zkServers = "127.0.0.1:2181";

        public static T WaitUntil<T>(T expectedValue, Func<T> callable, TimeSpan timeout)
        {
            var now = DateTime.Now;
            do
            {
                T actual = callable();
                if (expectedValue.Equals(actual))
                {
                    return actual;
                }
                if ((DateTime.Now - now) > timeout)
                {
                    return actual;
                }
                Thread.Sleep(50);
            } while (true);

        }

        public static async Task ReSetPathCreate(ZKClient _zkClient, string path)
        {
            if (!(await _zkClient.ExistsAsync(path)))
            {
                await _zkClient.CreatePersistentAsync(path);
            }
            else
            {
                await _zkClient.DeleteRecursiveAsync(path);
                await _zkClient.CreatePersistentAsync(path);
            }
        }

        public static async Task ReSetPathUnCreate(ZKClient _zkClient, string path)
        {
            var children = await _zkClient.GetChildrenAsync("/");
            children.Where(x => x != "zookeeper").ToList().ForEach(async x =>
                 {
                     await _zkClient.DeleteRecursiveAsync("/" + x);
                 });
            if (await _zkClient.ExistsAsync(path))
            {
                await _zkClient.DeleteRecursiveAsync(path);
            }
        }

    }
}
