using NeutronNetwork.Interfaces;
using System;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Internal.Wrappers
{
    [Serializable]
    public class NeutronSafeSerializableDictionary<TValue> : NeutronSafeDictionary<int, TValue>, ISerializationCallbackReceiver where TValue : INeutron
    {
        [SerializeField] private TValue[] m_Values;
        public void OnAfterDeserialize()
        {
            base.Clear();
            for (int i = 0; i < m_Values.Length; i++)
            {
                if (!base.ContainsKey(m_Values[i].Id))
                    base.TryAdd(m_Values[i].Id, m_Values[i]);
                else
                {
                    m_Values[i].Id = i;
                    base.TryAdd(m_Values[i].Id, m_Values[i]);
                }
            }
        }

        public void OnBeforeSerialize() => m_Values = base.Values.ToArray();

        public bool Add(TValue value)
        {
            return base.TryAdd(value.Id, value);
        }
    }
}