using System.Collections;
using System.Collections.Generic;
using NeutronNetwork;
using UnityEngine;

namespace NeutronNetwork.Examples.DeltaEx
{
    public class SyncVarTest : SyncVarBehaviour
    {
        [SyncVar] public float health = 100;
        [SyncVar] public int level = 1;
    }
}