using System;
using System.Reflection;
using NeutronNetwork;
using UnityEngine;

public class RPC
{
    #region Fields
    private MonoBehaviour _instance;
    private MethodInfo _method;
    private Type _type;
    private MethodType methodType;
    #endregion

    #region Properties
    public iRPC IRPC { get; }
    public gRPC GRPC { get; }
    #endregion

    #region Delegates iRPC
    private Action<NeutronReader, bool, NeutronPlayer> iRPCVoid;
    #endregion

    #region Delegates gRPC
    private Action<NeutronReader, bool, bool, NeutronPlayer, Neutron> gRPCVoid;
    private Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, bool> gRPCBool;
    private Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, NeutronView> gRPCView;
    #endregion

    public RPC(MonoBehaviour instance, MethodInfo method, iRPC iRPC)
    {
        _instance = instance;
        _method = method;
        _type = method.ReturnType;
        IRPC = iRPC;
        ////////////// Delegate ////////////////
        CreateDelegates(IRPC);
    }

    public RPC(MonoBehaviour instance, MethodInfo method, gRPC gRPC)
    {
        _instance = instance;
        _method = method;
        _type = method.ReturnType;
        GRPC = gRPC;
        ////////////// Delegate ////////////////
        CreateDelegates(GRPC);
    }

    private void CreateDelegates(Attribute attribute)
    {
        try
        {
            if (attribute is iRPC)
            {
                if (_type == typeof(void))
                {
                    iRPCVoid = (Action<NeutronReader, bool, NeutronPlayer>)_method.CreateDelegate(typeof(Action<NeutronReader, bool, NeutronPlayer>), _instance);
                    methodType = MethodType.Void;
                }
            }
            else if (attribute is gRPC)
            {
                if (_type == typeof(void))
                {
                    gRPCVoid = (Action<NeutronReader, bool, bool, NeutronPlayer, Neutron>)_method.CreateDelegate(typeof(Action<NeutronReader, bool, bool, NeutronPlayer, Neutron>), _instance);
                    methodType = MethodType.Void;
                }
                else if (_type == typeof(bool))
                {
                    gRPCBool = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, bool>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, bool>), _instance);
                    methodType = MethodType.Bool;
                }
                else if (_type == typeof(NeutronView))
                {
                    gRPCView = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, NeutronView>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, NeutronView>), _instance);
                    methodType = MethodType.View;
                }
                else
                    LogHelper.Error($"Type not supported! {_type}");
            }
            else
                LogHelper.Error($"Attribute not supported!");
        }
        catch
        {
            LogHelper.Error($"Arguments are out of order or their types are wrong. {attribute.GetType().Name}[{_method.Name}]");
        }
    }

    /// <summary>
    /// Invoca o iRPC.
    /// </summary>
    public object Invoke(NeutronReader reader, bool isMine, NeutronPlayer player)
    {
        switch (methodType)
        {
            case MethodType.Void:
                {
                    iRPCVoid(reader, isMine, player);
                    return null;
                }
            default:
                {
                    if (!LogHelper.Error("Type not supported! [iRPC]"))
                        return null;
                    else
                        return null;
                }
        }
    }

    /// <summary>
    /// Invoca o gRPC.
    /// </summary>
    public object Invoke(NeutronReader reader, bool isServer, bool isMine, NeutronPlayer player, Neutron neutron)
    {
        switch (methodType)
        {
            case MethodType.Void:
                {
                    gRPCVoid(reader, isServer, isMine, player, neutron);
                    return null;
                }
            case MethodType.Bool:
                return gRPCBool(reader, isServer, isMine, player, neutron);
            case MethodType.View:
                return gRPCView(reader, isServer, isMine, player, neutron);
            default:
                {
                    if (!LogHelper.Error("Type not supported! [gRPC]"))
                        return null;
                    else
                        return null;
                }
        }
    }

    private enum MethodType
    {
        Void,
        Bool,
        View,
        Object,
        String
    }
}