namespace ZkClientNET
{
    public interface IZkDataListener
    {
        void HandleDataChange(string dataPath, object data);

        void HandleDataDeleted(string dataPath);
    }
}