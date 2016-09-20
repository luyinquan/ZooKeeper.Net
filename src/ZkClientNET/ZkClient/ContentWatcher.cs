using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient
{
    public class ContentWatcher<T> : IZkDataListener
    {
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
