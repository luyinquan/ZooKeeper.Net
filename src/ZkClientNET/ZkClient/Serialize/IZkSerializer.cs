using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient.Serialize
{
    public interface IZkSerializer
    {
        byte[] Serialize(object data);

        object Deserialize(byte[] bytes);
    }
}
