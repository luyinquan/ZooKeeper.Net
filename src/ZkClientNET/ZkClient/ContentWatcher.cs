using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZkClientNET.ZkClient.Exceptions;

namespace ZkClientNET.ZkClient
{
    public class ContentWatcher<T> : IZkDataListener
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ContentWatcher<T>));
        private int _contentLock = 0;
        private Holder<T> _content;
        private string _fileName;
        private ZkClient _zkClient;

        public ContentWatcher(ZkClient zkClient, string fileName)
        {
            _fileName = fileName;
            _zkClient = zkClient;
        }

        public void Start()
        {
            _zkClient.SubscribeDataChanges(_fileName, this);
            LOG.Debug("Started ContentWatcher");
        }

        public void HandleDataChange(string dataPath, object data)
        {
            throw new NotImplementedException();
        }

        public void HandleDataDeleted(string dataPath)
        {
            throw new NotImplementedException();
        }
    }
}
