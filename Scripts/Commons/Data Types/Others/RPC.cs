using NeutronNetwork;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public class RPC
{
    #region Fields
    private MonoBehaviour _instance;
    private MethodInfo _method;
    private Type _type;
    private MethodType _methodType;
    #endregion

    #region Properties
    public iRPC IRPC { get; }
    public gRPC GRPC { get; }
    public MethodType Type => _methodType;
    #endregion

    #region Delegates iRPC
    private Action<NeutronReader, bool, NeutronPlayer> iRPCVoid;
    ////////////////////////////////// Funcs /////////////////////////////////////////////////////
    #endregion

    #region Delegates gRPC
    private Action<NeutronReader, bool, bool, NeutronPlayer, Neutron> gRPCVoid;
    ////////////////////////////////// Funcs /////////////////////////////////////////////////////
    private Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<int>> gRPCIntAsync;
    private Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<bool>> gRPCBoolAsync;
    private Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<NeutronView>> gRPCViewAsync;

    private Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, int> gRPCInt;
    private Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, bool> gRPCBool;
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
                    _methodType = MethodType.Void;
                }
            }
            else if (attribute is gRPC)
            {
                if (_type == typeof(void))
                {
                    gRPCVoid = (Action<NeutronReader, bool, bool, NeutronPlayer, Neutron>)_method.CreateDelegate(typeof(Action<NeutronReader, bool, bool, NeutronPlayer, Neutron>), _instance);
                    _methodType = MethodType.Void;
                }
                else if (_type == typeof(int))
                {
                    gRPCInt = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, int>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, int>), _instance);
                    _methodType = MethodType.Int;
                }
                else if (_type == typeof(bool))
                {
                    gRPCBool = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, bool>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, bool>), _instance);
                    _methodType = MethodType.Bool;
                }
                else if (_type == typeof(Task<int>))
                {
                    gRPCIntAsync = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<int>>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<int>>), _instance);
                    _methodType = MethodType.Async | MethodType.Int;
                }
                else if (_type == typeof(Task<bool>))
                {
                    gRPCBoolAsync = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<bool>>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<bool>>), _instance);
                    _methodType = MethodType.Async | MethodType.Bool;
                }
                else if (_type == typeof(Task<NeutronView>))
                {
                    gRPCViewAsync = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<NeutronView>>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<NeutronView>>), _instance);
                    _methodType = MethodType.Async | MethodType.View;
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
    /// Invoca o iRPC de modo assícrnono.
    /// </summary>
    public object Invoke(NeutronReader reader, bool isMine, NeutronPlayer player)
    {
        switch (_methodType)
        {
            case MethodType.Void:
                {
                    iRPCVoid(reader, isMine, player);
                    return null;
                }
            default:
                return null;
        }
    }

    /// <summary>
    /// Invoca o gRPC de modo assícrnono.
    /// </summary>
    public async Task<object> Invoke(NeutronReader reader, bool isServer, bool isMine, NeutronPlayer player, Neutron neutron)
    {
        switch (_methodType)
        {
            case MethodType.Void:
                {
                    gRPCVoid(reader, isServer, isMine, player, neutron);
                    return null;
                }
            case MethodType.Int:
                return gRPCInt(reader, isServer, isMine, player, neutron);
            case MethodType.Bool:
                return gRPCBool(reader, isServer, isMine, player, neutron);
            case MethodType.Async | MethodType.Bool:
                return await gRPCBoolAsync(reader, isServer, isMine, player, neutron);
            case MethodType.Async | MethodType.View:
                return await gRPCViewAsync(reader, isServer, isMine, player, neutron);
            case MethodType.Async | MethodType.Int:
                return await gRPCIntAsync(reader, isServer, isMine, player, neutron);
            default:
                return null;
        }
    }
}