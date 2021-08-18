namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronStream
    {
        byte[] ToArray();
        byte[] GetBuffer();
        void SetPosition(int position);
        long GetPosition();
        int GetCapacity();
        bool IsFixedSize();
        void SetCapacity(int size);
        void Clear();
        void Close();
    }
}