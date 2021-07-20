using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/Synchronization", fileName = "Neutron Synchronization")]
    public class Synchronization : ScriptableObject
    {
        [InfoBox("These properties are used on the server side.")] public NHandlerSettings SynchronizationHandlers;
        public NDefaultHandlerSettings DefaultHandlers;
    }
}