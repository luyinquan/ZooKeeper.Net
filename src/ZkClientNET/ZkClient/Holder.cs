using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZkClientNET.ZkClient
{
    public class Holder<T>
    {
        public Holder(T value)
        {
            _value = value;
        }

        public T _value { set; get; }
    }
}
