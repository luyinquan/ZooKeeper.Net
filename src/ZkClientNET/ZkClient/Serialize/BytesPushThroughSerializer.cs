using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient.Serialize
{
    public class BytesPushThroughSerializer : IZkSerializer
    {
        public byte[] Serialize(object bytes)
        {
            return (byte[])bytes;
        }

        public object Deserialize(byte[] bytes)
        {
            return bytes;
        }
    }
}
