namespace ZookeeperClient.Serialize
{
    public interface IZKSerializer
    {
        byte[] Serialize(object data);

        object Deserialize(byte[] bytes);
    }
}
