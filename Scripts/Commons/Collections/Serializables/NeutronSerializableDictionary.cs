using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NeutronNetwork;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Interfaces;

namespace NeutronNetwork.Internal.Wrappers
{
    [Serializable]
    public class NeutronSerializableDictionary<TValue> : Dictionary<int, TValue>, ISerializationCallbackReceiver where TValue : INeutronNotify
    {
        [SerializeField] private TValue[] m_Values;
        public void OnAfterDeserialize()
        {
            base.Clear();
            for (int i = 0; i < m_Values.Length; i++)
                if (!base.ContainsKey(m_Values[i].ID))
                    base.Add(m_Values[i].ID, m_Values[i]);
                else
                {
                    m_Values[i].ID = i;
                    base.Add(m_Values[i].ID, m_Values[i]);
                }
        }

        public void OnBeforeSerialize() => m_Values = base.Values.ToArray();

        public void Add(TValue value)
        {
            base.Add(value.ID, value);
        }
    }
}