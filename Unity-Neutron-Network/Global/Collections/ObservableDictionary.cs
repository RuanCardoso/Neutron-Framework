using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Wrappers
{
    [Serializable]
    public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private event ObserverDelegates.OnChanged onChanged;
        private string fieldName;

        public ObservableDictionary(string fieldName)
        {
            this.fieldName = fieldName;
        }

        public new void Add(TKey key, TValue item)
        {
            base.Add(key, item);
            onChanged?.Invoke(fieldName);
        }

        public new void Remove(TKey key)
        {
            if (base.Remove(key))
                onChanged?.Invoke(fieldName);
        }

        public new TValue this[TKey key]
        {
            get => base[key];
            set
            {
                base[key] = value;
                onChanged?.Invoke(fieldName);
            }
        }
    }
}