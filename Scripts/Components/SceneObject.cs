using NeutronNetwork;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Packets;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrder.NEUTRON_REGISTER)]
public class SceneObject : MonoBehaviour
{
    private NeutronView NeutronView;
    public static NeutronEventNoReturn<Neutron> OnRegister { get; set; }
    private void Awake()
    {
        NeutronView = GetComponent<NeutronView>();
#if UNITY_EDITOR && !UNITY_SERVER
        OnRegister += (neutron) =>
        {
            NeutronSchedule.ScheduleTask(Init(neutron));
        };
#endif
    }

    private IEnumerator Init(Neutron neutron)
    {
        yield return new WaitUntil(() => !NeutronView.IsServer);
        NeutronView.OnNeutronRegister(neutron.Player, false, RegisterMode.Scene, neutron);
    }
}