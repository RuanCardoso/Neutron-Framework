using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeutronList<T> : List<T>
{
    public delegate void OnChanged();
    public static event OnChanged onChanged;
}
