namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronConsumer<T>
    {
        void Push(T item, bool sort = false);
        T Pull();
        bool TryPull(out T item);
        void Dispose();
    }
}