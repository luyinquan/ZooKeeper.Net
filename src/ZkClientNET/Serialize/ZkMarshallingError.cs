using System;
using System.IO;
using System.Runtime.Serialization;

namespace ZkClientNET.Serialize
{
    [Serializable]
    internal class ZkMarshallingError : Exception
    {
        private IOException e;

        public ZkMarshallingError()
        {
        }

        public ZkMarshallingError(string message) : base(message)
        {
        }

        public ZkMarshallingError(IOException e)
        {
            this.e = e;
        }

        public ZkMarshallingError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZkMarshallingError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}