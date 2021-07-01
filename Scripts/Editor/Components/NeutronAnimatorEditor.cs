using System.Linq;
using System.Reflection;
using NeutronNetwork.Client.Internal;
using NeutronNetwork.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(NeutronAnimator))]
public class NeutronAnimatorEditor : Editor
{
    private NeutronAnimator neutronAnimatorTarget;

    private void OnEnable()
    {
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
                    neutronAnimatorTarget.m_Parameters = controller.parameters.Select(x => new NeutronAnimatorParameter(x.name, x.type, ParameterMode.Sync)).ToArray();
            }
        }
    }
}