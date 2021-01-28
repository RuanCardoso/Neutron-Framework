using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class NeutronQueueData : ConcurrentQueue<byte[]>
{
    public delegate void OnChanged();
    public event OnChanged onChanged;
    public new void Enqueue(byte[] data)
    {
        base.Enqueue(data);
        if (onChanged != null) onChanged();
    }
}