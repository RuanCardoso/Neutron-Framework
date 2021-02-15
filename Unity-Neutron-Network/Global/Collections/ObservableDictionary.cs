using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Wrappers
{
    [Serializable]
    public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public delegate void OnChanged();
        public event OnChanged onChanged;
        public new void Add(TKey key, TValue item)
        {
            base.Add(key, item);
            onChanged?.Invoke();
        }

        public new void Remove(TKey key)
        {
            if (base.Remove(key))
                onChanged?.Invoke();
        }

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                onChanged?.Invoke();
            }
        }
    }
}