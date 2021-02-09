using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Wrappers
{
    [Serializable]
    public class ObservableList<T> : List<T>
    {
        public delegate void OnChanged();
        public event OnChanged onChanged;
        public new void Add(T item)
        {
            base.Add(item);
            onChanged?.Invoke();
        }

        public new void Remove(T item)
        {
            if (base.Remove(item))
                onChanged?.Invoke();
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            onChanged?.Invoke();
        }

        public new T this[int index] {
            get => base[index];
            set {
                base[index] = value;
                onChanged?.Invoke();
            }
        }
    }
}