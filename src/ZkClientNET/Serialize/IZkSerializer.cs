using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.Serialize
{
    public interface IZkSerializer
    {
        byte[] Serialize(object data);

        object Deserialize(byte[] bytes);
    }
}
