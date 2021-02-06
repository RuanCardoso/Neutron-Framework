using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Physics Ignore")]
    public class NeutronPhysicsIgnore : MonoBehaviour
    {
        public bool ignore = true;
        private void OnEnable()
        {
            if (Application.isEditor)
            {
                int layerMask = LayerMask.NameToLayer("ClientObject");
                int layerMask2 = LayerMask.NameToLayer("ServerObject");
                Physics.IgnoreLayerCollision(layerMask, layerMask2, ignore);
            }
        }
    }
}