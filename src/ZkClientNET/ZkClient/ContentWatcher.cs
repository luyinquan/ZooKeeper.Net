using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZkClientNET.ZkClient.Exceptions;

namespace ZkClientNET.ZkClient
{
    public class ContentWatcher<T> : IZkDataListener
    {
        private static readonly ILog LOG = LogManager.GetLogger(typeof(ContentWatcher<T>));
        private int _contentLock =0;

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

        private void ReadData()
        {
            try
            {
                SetContent(_zkClient.ReadData<T>(_fileName));
            }
            catch (ZkNoNodeException e)
            {
                // ignore if the node has not yet been created
            }
        }

        public void Stop()
        {
            _zkClient.UnSubscribeDataChanges(_fileName, this);
        }

        private void SetContent(T data)
        {
            LOG.Debug("Received new data: " + data);
            if (0 == Interlocked.Exchange(ref _contentLock, 1))
            {
                _content = new Holder<T>(data);
            }
        }

        public void HandleDataChange(string dataPath, object data)
        {
            SetContent((T)data);
        }

        public void HandleDataDeleted(string dataPath)
        {
            throw new NotImplementedException();
        }

        public T GetContent()
        {
            while (_content == null)
            {
                Interlocked.Exchange(ref _contentLock, 0);
            }
            return _content._value;
        }

    }
}
