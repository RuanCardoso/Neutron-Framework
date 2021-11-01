using NeutronNetwork.Naughty.Attributes;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Authority")]
    public class NeutronAuthority : NeutronBehaviour
    {
        [OnValueChanged("Set")] public bool _root;
        protected override void Reset()
        {
            base.Reset();
            {
#if UNITY_EDITOR
                Set();
#endif
            }
        }

        private void Set()
        {
#if UNITY_EDITOR
            var neutronAuthorities = transform.root.GetComponentsInChildren<NeutronAuthority>().Where(x => x._root).ToArray();
            if (neutronAuthorities.Length > 0)
            {
                var internAuthorities = transform.root.GetComponentsInChildren<NeutronAuthority>();
                if (internAuthorities.Length > 1)
                    LogHelper.Error("Only one authority controller can exist when root mode is active.");
            }

            NeutronBehaviour[] neutronBehaviours = !_root ? transform.GetComponents<NeutronBehaviour>() : transform.root.GetComponentsInChildren<NeutronBehaviour>();
            foreach (var neutronBehaviour in neutronBehaviours)
            {
                if (neutronBehaviour != this)
                    neutronBehaviour.HandledBy(this);
            }
#endif
        }
    }
}