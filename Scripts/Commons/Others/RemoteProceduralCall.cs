using System;
using System.Reflection;
using NeutronNetwork;
using UnityEngine;

public class RemoteProceduralCall
{
    public MonoBehaviour instance { get; }
    public MethodInfo method { get; }
    public Attribute attribute { get; }

    #region Delegates iRPC
    Action<NeutronReader, bool, Player> VoidDynamic;
    #endregion

    #region Delegates sRPC
    readonly Action<NeutronReader, bool, bool, Player, Neutron> VoidNonDynamic;
    readonly Func<NeutronReader, bool, bool, Player, Neutron, bool> BoolNonDynamic;
    readonly Func<NeutronReader, bool, bool, Player, Neutron, NeutronView> NeutronViewNonDynamic;
    #endregion
    public RemoteProceduralCall(MonoBehaviour instance, MethodInfo method, Attribute attribute)
    {
        this.instance = instance;
        this.method = method;
        this.attribute = attribute;

        #region Register
        try
        {
            if (attribute is iRPC)
            {
                if (method.ReturnType == typeof(void))
                    VoidDynamic = (Action<NeutronReader, bool, Player>)Delegate.CreateDelegate(typeof(Action<NeutronReader, bool, Player>), instance, method);
                else
                {

                }
            }
            else if (attribute is sRPC)
            {
                if (method.ReturnType == typeof(void))
                    VoidNonDynamic = (Action<NeutronReader, bool, bool, Player, Neutron>)Delegate.CreateDelegate(typeof(Action<NeutronReader, bool, bool, Player, Neutron>), instance, method);
                else if (method.ReturnType == typeof(bool))
                    BoolNonDynamic = (Func<NeutronReader, bool, bool, Player, Neutron, bool>)Delegate.CreateDelegate(typeof(Func<NeutronReader, bool, bool, Player, Neutron, bool>), instance, method);
                else if (method.ReturnType == typeof(NeutronView))
                    NeutronViewNonDynamic = (Func<NeutronReader, bool, bool, Player, Neutron, NeutronView>)Delegate.CreateDelegate(typeof(Func<NeutronReader, bool, bool, Player, Neutron, NeutronView>), instance, method);
                else NeutronLogger.LoggerError($"Type RPC Not supported! {method.ReturnType}");
            }
            else NeutronLogger.LoggerError($"Type of attribue not supported!");
        }
        catch { NeutronLogger.LoggerError($"Parameter order or types are incorrect. {attribute.GetType().Name}[{method.Name}]"); }
        #endregion
    }

    public object Invoke(NeutronReader reader, bool isMine, Player player) // dynamic
    {
        Type type = method.ReturnType;
        if (type == typeof(void))
            VoidDynamic(reader, isMine, player);
        else NeutronLogger.LoggerError("Type not supported");
        #region Return
        return null;
        #endregion
    }

    public object Invoke(NeutronReader reader, bool isServer, bool isMine, Player player, Neutron neutron) // non dynamic
    {
        Type type = method.ReturnType;
        if (type == typeof(void))
            VoidNonDynamic(reader, isServer, isMine, player, neutron);
        else if (type == typeof(bool))
            return BoolNonDynamic(reader, isServer, isMine, player, neutron);
        else if (type == typeof(NeutronView))
            return NeutronViewNonDynamic(reader, isServer, isMine, player, neutron);
        else NeutronLogger.LoggerError("Type not supported");
        #region Return
        return null;
        #endregion
    }
}