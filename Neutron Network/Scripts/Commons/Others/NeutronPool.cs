using NeutronNetwork.Internal.Wrappers;
using System;
using UnityEngine;

public class NeutronPool<T> : MonoBehaviour
{
    #region Struct
    private readonly NeutronSafeQueue<T> objects = new NeutronSafeQueue<T>();
    private readonly Func<T> objectGenerator;

    public NeutronPool(Func<T> objectGenerator)
    {
        this.objectGenerator = objectGenerator;
    }
    #endregion

    #region Methods

    public T Pull()
    {
        if (Count() > 0)
        {
            if (objects.TryDequeue(out T obj))
                return obj;
            else return objectGenerator();
        }
        else return objectGenerator();
    }
    public void Push(T obj) => objects.Enqueue(obj);
    public int Count() => objects.Count;
    #endregion
}