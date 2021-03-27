using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Wrappers
{
    [Serializable]
    public class ObservableList<T> : List<T>
    {
        private event ObserverDelegates.OnChanged onChanged;
        private string fieldName;
        public ObservableList() { }

        public ObservableList(string fieldName)
        {
            this.fieldName = fieldName;
        }

        public new void Add(T item)
        {
            base.Add(item);
            onChanged?.Invoke(fieldName);
        }

        public new void Remove(T item)
        {
            if (base.Remove(item))
                onChanged?.Invoke(fieldName);
        }

        public new void RemoveAt(int index)
        {
            base.RemoveAt(index);
            onChanged?.Invoke(fieldName);
        }

        public new T this[int index]
        {
            get => base[index];
            set
            {
                base[index] = value;
                onChanged?.Invoke(fieldName);
            }
        }
    }
}