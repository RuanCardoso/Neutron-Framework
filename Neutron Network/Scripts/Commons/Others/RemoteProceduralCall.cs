using System.Reflection;
using NeutronNetwork;
using UnityEngine;

public class RemoteProceduralCall
{
    public MonoBehaviour instance;
    public MethodInfo method;
    public RemoteProceduralCall(MonoBehaviour instance, MethodInfo method)
    {
        this.instance = instance;
        this.method = method;
    }

    public object Invoke(params object[] parameters)
    {
        return method.Invoke(instance, parameters);
    }
}