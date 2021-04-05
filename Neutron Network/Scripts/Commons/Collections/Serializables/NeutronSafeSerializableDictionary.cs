using System;
using System.Collections.Generic;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;
using UnityEngine;

[Serializable]
public class NeutronSafeSerializableDictionary<TKey, TValue> : NeutronSafeDictionary<TKey, TValue>, ISerializationCallbackReceiver where TValue : INeutronNotify
{
    [SerializeField] private List<ListValue<TKey, TValue>> m_list = new List<ListValue<TKey, TValue>>();

    public void OnAfterDeserialize() => AddToDict<TValue>();
    public void OnBeforeSerialize() { }

    private void AddToDict<T>() where T : INeutronNotify
    {
        base.Clear();
        for (int i = 0; i < m_list.Count; i++)
        {
            m_list[i].key = (TKey)Convert.ChangeType(m_list[i].value.ID, typeof(TKey));
            if (!base.ContainsKey(m_list[i].key))
                base.TryAdd(m_list[i].key, m_list[i].value);
            else return;
        }
    }

    public new bool TryAdd(TKey key, TValue value)
    {
        bool TryValue = false;
        if ((TryValue = base.TryAdd(key, value)))
#if UNITY_EDITOR
            m_list.Add(new ListValue<TKey, TValue>(key, value));
#else
        { /* gambiarra só pra não ter que colocar chaves no if*/ }
#endif
        return TryValue;
    }

    public new bool TryRemove(TKey key, out TValue value) => base.TryRemove(key, out value) && m_list.Remove(new ListValue<TKey, TValue>(key));

    public new void Clear()
    {
        base.Clear();
#if UNITY_EDITOR
        m_list.Clear();
#endif
    }
}