using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ZKClientNET.Serialize
{
    public class SerializableSerializer : IZKSerializer
    {
        public byte[] Serialize(object obj)
        {
                using (MemoryStream ms = new MemoryStream())
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(ms, obj);
                    return ms.GetBuffer();
                }
        }

        public object Deserialize(byte[] bytes)
        {
                using (MemoryStream ms = new MemoryStream(bytes))
                {
                    IFormatter formatter = new BinaryFormatter();
                    return formatter.Deserialize(ms);
                }
        }
    }
}
