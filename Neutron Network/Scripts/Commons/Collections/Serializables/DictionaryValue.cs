using System;
using NeutronNetwork.Internal.Attributes;

[Serializable]
public class ListValue<TKey, TValue> : IEquatable<ListValue<TKey, TValue>>
{
    [ReadOnly] public TKey key;
    public TValue value;

    public ListValue(TKey key)
    {
        this.key = key;
    }

    public ListValue(TKey key, TValue value)
    {
        this.key = key;
        this.value = value;
    }

    public bool Equals(ListValue<TKey, TValue> other)
    {
        return this.key.Equals(other.key);
    }
}