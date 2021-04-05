using System;
using System.Collections;
using System.Collections.Generic;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;
using UnityEngine;

[Serializable]
public class NeutronSerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver where TValue : INeutronNotify
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
                base.Add(m_list[i].key, m_list[i].value);
            else return;
        }
    }

    public new void Add(TKey key, TValue value)
    {
        base.Add(key, value);
#if UNITY_EDITOR
        m_list.Add(new ListValue<TKey, TValue>(key, value));
#endif
    }

    public new bool Remove(TKey key) => base.Remove(key) && m_list.Remove(new ListValue<TKey, TValue>(key));

    public new void Clear()
    {
        base.Clear();
#if UNITY_EDITOR
        m_list.Clear();
#endif
    }
}