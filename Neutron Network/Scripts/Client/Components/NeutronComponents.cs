using NeutronNetwork;
using NeutronNetwork.Client.Internal;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Components")]
    public class NeutronComponents : NeutronBehaviour
    {
        [SerializeField] private NeutronComponent[] neutronComponents;
        private void Start() { }

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            DisallowComponents();
        }

        private void DisallowComponents()
        {
            foreach (var component in neutronComponents)
            {
                const ComponentMode ComponentMode = default;
                switch (component.componentMode)
                {
                    case ComponentMode.IsMine:
                        if (HasAuthority) { }
                        else Destroy(component);
                        break;
                    case ComponentMode.IsServer:
                        if (IsServer) { }
                        else Destroy(component);
                        break;
                    case ComponentMode:
                        Destroy(component);
                        break;
                    default:
                        if (IsServer) { }
                        else if (!HasAuthority) Destroy(component);
                        break;
                }
            }
            Destroy(this);
        }

        private void Destroy(NeutronComponent component)
        {
            if (component.component != null)
                Destroy(component.component);
            if (component.gameObject != null)
                Destroy(component.gameObject);
        }
    }
}