using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeutronNetwork.Components;
using Supyrb;
using UnityEditor;
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
        int parametersCount = neutronAnimatorTarget.animator.parameterCount;
        if (parametersCount == 0 && neutronAnimatorTarget.animator.isActiveAndEnabled)
            AnimatorRefresh();
        if (neutronAnimatorTarget.parameters != null)
        {
            if (parametersCount > 0 && neutronAnimatorTarget.parameters.Length != parametersCount && neutronAnimatorTarget.animator.isActiveAndEnabled)
                ParametersUpdate();
        }
        else ParametersUpdate();
    }

    private void AnimatorRefresh()
    {
        neutronAnimatorTarget.animator.enabled = false;
        neutronAnimatorTarget.animator.enabled = true;
    }

    private void ParametersUpdate()
    {
        neutronAnimatorTarget.parameters = neutronAnimatorTarget.animator.parameters.Select(x => new NeutronAnimatorParameter(x.name, x.type, ParameterMode.Sync)).ToArray();
    }
}