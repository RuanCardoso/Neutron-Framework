using System.Collections;
using System.Collections.Generic;
using NeutronNetwork;
using UnityEngine;

public class NeutronComponents : NeutronBehaviour
{
    [SerializeField] private NeutronComponent[] neutronComponents;

    private void Start() { }
    public override void OnNeutronStart()
    {
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
                    if (IsMine) { }
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
                    else if (!IsMine) Destroy(component);
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