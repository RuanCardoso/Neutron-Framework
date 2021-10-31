namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronConsumer<T>
    {
        public void Push(T item);
        public T Pull();
        public bool TryPull(out T item);
        public void Dispose();
    }
}