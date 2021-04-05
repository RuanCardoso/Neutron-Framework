using System.Linq;
using System.Reflection;
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
        if (neutronAnimatorTarget.animator == null)
            neutronAnimatorTarget.animator = neutronAnimatorTarget.GetComponent<Animator>();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (neutronAnimatorTarget.animator != null)
        {
            AnimatorController controller = (AnimatorController)neutronAnimatorTarget.animator.runtimeAnimatorController;
            if (controller != null)
            {
                if (neutronAnimatorTarget.parameters.Length != controller.parameters.Length)
                    neutronAnimatorTarget.parameters = controller.parameters.Select(x => new NeutronAnimatorParameter(x.name, x.type, ParameterMode.Sync)).ToArray();
            }
        }
    }
}