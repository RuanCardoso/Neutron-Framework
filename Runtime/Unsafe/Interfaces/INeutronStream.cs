using System.IO;

namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronStream
    {
        byte[] ToArray();
        byte[] GetBuffer();
        MemoryStream AsStream();
        void SetPosition(int position);
        long GetPosition();
        int GetCapacity();
        bool IsFixedSize();
        void SetCapacity(int size);
        void Reset();
        void Close();
    }
}