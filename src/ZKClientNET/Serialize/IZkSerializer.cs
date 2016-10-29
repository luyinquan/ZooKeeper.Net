using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZKClientNET.Serialize
{
    public interface IZKSerializer
    {
        byte[] Serialize(object data);

        object Deserialize(byte[] bytes);
    }
}
