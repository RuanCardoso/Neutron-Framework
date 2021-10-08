using NeutronNetwork.Components;
using NeutronNetwork.Editor;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Naughty.Attributes.Editor;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(NeutronAnimator))]
public class NeutronAnimatorEditor : NaughtyInspector
{
    private NeutronAnimator neutronAnimatorTarget;

    protected override void OnEnable()
    {
        base.OnEnable();
        neutronAnimatorTarget = (NeutronAnimator)target;
        if (neutronAnimatorTarget.m_Animator == null)
            neutronAnimatorTarget.m_Animator = neutronAnimatorTarget.GetComponent<Animator>();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (neutronAnimatorTarget.m_Animator != null)
        {
            AnimatorController controller = (AnimatorController)neutronAnimatorTarget.m_Animator.runtimeAnimatorController;
            if (controller != null)
            {
                if (neutronAnimatorTarget.m_Parameters.Length != controller.parameters.Length)
                    neutronAnimatorTarget.m_Parameters = controller.parameters.Select(x => new AnimatorParameter(x.name, x.type, SyncOnOff.Sync)).ToArray();
            }
        }
    }
}