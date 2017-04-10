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
