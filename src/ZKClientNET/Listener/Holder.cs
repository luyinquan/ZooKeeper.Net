using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZKClientNET.Listener
{
    public class Holder<T>
    {
        public Holder()
        {
        }

        public Holder(T value)
        {
            this.value = value;
        }

        public T value { set; get; }
    }
}
