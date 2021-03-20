using System;
using UnityEngine;

[Serializable]
public class NeutronAnimatorParameter : IEquatable<NeutronAnimatorParameter>
{
    public string parameterName;
    public AnimatorControllerParameterType parameterType;
    public ParameterMode parameterMode;

    public NeutronAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType, ParameterMode parameterMode)
    {
        this.parameterName = parameterName;
        this.parameterMode = parameterMode;
        this.parameterType = parameterType;
    }

    public bool Equals(NeutronAnimatorParameter other)
    {
        return this.parameterName == other.parameterName;
    }
}