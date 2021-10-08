using NeutronNetwork.Constants;
using UnityEngine;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/Synchronization", fileName = "Neutron Synchronization")]
    public class Synchronization : ScriptableObject
    {
        public NeutronDefaultHandlerSettings DefaultHandlers;
    }
}