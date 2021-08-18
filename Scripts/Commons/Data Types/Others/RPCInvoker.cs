using NeutronNetwork.Internal.Packets;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    public class RPCInvoker
    {
        #region Fields
        private MonoBehaviour _instance;
        private MethodInfo _method;
        private Type _type;
        private MethodType _methodType;
        #endregion

        #region Properties
#pragma warning disable IDE1006
        public iRPC iRPC { get; }
#pragma warning restore IDE1006
#pragma warning disable IDE1006
        public gRPC gRPC { get; }
#pragma warning restore IDE1006
        public MethodType Type => _methodType;
        #endregion

        #region Delegates iRPC
#pragma warning disable IDE1006
        public Action<NeutronReader, NeutronPlayer> iRPCVoid { get; set; }
        public Func<NeutronReader, NeutronPlayer, Task> iRPCTaskAsync { get; set; }
        public Func<NeutronReader, NeutronPlayer, Task<bool>> iRPCBoolAsync { get; set; }
        public Func<NeutronReader, NeutronPlayer, bool> iRPCBool { get; set; }
#pragma warning restore IDE1006
        #endregion

        #region Delegates gRPC
#pragma warning disable IDE1006
        public Action<NeutronReader, bool, bool, NeutronPlayer, Neutron> gRPCVoid { get; set; }
        public Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task> gRPCTaskAsync { get; set; }
        public Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<int>> gRPCIntAsync { get; set; }
        public Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<bool>> gRPCBoolAsync { get; set; }
        public Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task<NeutronView>> gRPCViewAsync { get; set; }
        public Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, int> gRPCInt { get; set; }
        public Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, bool> gRPCBool { get; set; }
#pragma warning restore IDE1006
        #endregion

        public RPCInvoker(MonoBehaviour instance, MethodInfo method, iRPC iRPC)
        {
            _instance = instance;
            _method = method;
            _type = method.ReturnType;
            this.iRPC = iRPC;
            ////////////// Delegate ////////////////
            CreateDelegates(this.iRPC);
        }

        public RPCInvoker(MonoBehaviour instance, MethodInfo method, gRPC gRPC)
        {
            _instance = instance;
            _method = method;
            _type = method.ReturnType;
            this.gRPC = gRPC;
            ////////////// Delegate ////////////////
            CreateDelegates(this.gRPC);
        }

        private void CreateDelegates(Attribute attribute)
        {
            try
            {
                if (attribute is iRPC)
                {
                    if (_type == typeof(void))
                    {
                        iRPCVoid = (Action<NeutronReader, NeutronPlayer>)_method.CreateDelegate(typeof(Action<NeutronReader, NeutronPlayer>), _instance);
                        _methodType = MethodType.Void;
                    }
                    else if (_type == typeof(bool))
                    {
                        iRPCBool = (Func<NeutronReader, NeutronPlayer, bool>)_method.CreateDelegate(typeof(Func<NeutronReader, NeutronPlayer, bool>), _instance);
                        _methodType = MethodType.Bool;
                    }
                    else if (_type == typeof(Task))
                    {
                        iRPCTaskAsync = (Func<NeutronReader, NeutronPlayer, Task>)_method.CreateDelegate(typeof(Func<NeutronReader, NeutronPlayer, Task>), _instance);
                        _methodType = MethodType.Async | MethodType.Task;
                    }
                    else if (_type == typeof(Task<bool>))
                    {
                        iRPCBoolAsync = (Func<NeutronReader, NeutronPlayer, Task<bool>>)_method.CreateDelegate(typeof(Func<NeutronReader, NeutronPlayer, Task<bool>>), _instance);
                        _methodType = MethodType.Async | MethodType.Bool;
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
                    else if (_type == typeof(Task))
                    {
                        gRPCTaskAsync = (Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task>)_method.CreateDelegate(typeof(Func<NeutronReader, bool, bool, NeutronPlayer, Neutron, Task>), _instance);
                        _methodType = MethodType.Async | MethodType.Task;
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
    }
}