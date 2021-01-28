using System;
using System.Collections.Concurrent;
using UnityEngine;

public class ProcessEvents : MonoBehaviour
{
    public Neutron owner;

    public ConcurrentQueue<Action> MAO, MAT;
    public int DPF;

    private void Update()
    {
        if (owner == null) return;

        if (MAO!= null && MAT != null)
        {
            Utils.Dequeue(ref MAO, DPF);
            Utils.Dequeue(ref MAT, DPF);
        }
    }
}