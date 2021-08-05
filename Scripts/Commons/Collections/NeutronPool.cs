using System;

public class NeutronPool<T>
{
    private readonly NeutronSafeQueue<T> objects = new NeutronSafeQueue<T>();
    private readonly Func<T> objectGenerator;

    public int Count => objects.Count;

    public NeutronPool(Func<T> objectGenerator)
    {
        this.objectGenerator = objectGenerator;
    }

    public T Pull()
    {
        if (Count > 0)
        {
            if (objects.TryDequeue(out T obj))
                return obj;
            else
                return objectGenerator();
        }
        else
            return objectGenerator();
    }
    public void Push(T obj) => objects.Enqueue(obj);
}