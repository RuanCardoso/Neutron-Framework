using System;
using System.Reflection;
using NeutronNetwork;
using UnityEngine;

public class RemoteProceduralCall
{
    public MonoBehaviour instance;
    public MethodInfo method;
    public Attribute attribute;
    public RemoteProceduralCall(MonoBehaviour instance, MethodInfo method, Attribute attribute)
    {
        this.instance = instance;
        this.method = method;
        this.attribute = attribute;
    }

    public object Invoke(params object[] parameters)
    {
        return method.Invoke(instance, parameters);
    }
}