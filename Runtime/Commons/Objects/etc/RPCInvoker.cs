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
        private readonly MonoBehaviour _instance;
        private readonly MethodInfo _method;
        private readonly Type _type;
        private MethodType _methodType;
        #endregion

        #region Properties
#pragma warning disable IDE1006
        public iRPCAttribute iRPC {
            get;
        }
#pragma warning restore IDE1006
#pragma warning disable IDE1006
        public gRPCAttribute gRPC {
            get;
        }
#pragma warning restore IDE1006
        public MethodType Type => _methodType;
        #endregion

        #region Delegates iRPC
#pragma warning disable IDE1006
        public Action<NeutronStream.IReader, NeutronPlayer> iRPCVoid {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, NeutronPlayer, Task> iRPCTaskAsync {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, NeutronPlayer, Task<bool>> iRPCBoolAsync {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, NeutronPlayer, bool> iRPCBool {
            get;
            private set;
        }
#pragma warning restore IDE1006
        #endregion

        #region Delegates gRPC
#pragma warning disable IDE1006
        public Action<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron> gRPCVoid {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task> gRPCTaskAsync {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<int>> gRPCIntAsync {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<bool>> gRPCBoolAsync {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<NeutronView>> gRPCViewAsync {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, int> gRPCInt {
            get;
            private set;
        }
        public Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, bool> gRPCBool {
            get;
            private set;
        }
#pragma warning restore IDE1006
        #endregion

        public RPCInvoker(MonoBehaviour instance, MethodInfo method, iRPCAttribute iRPC)
        {
            _instance = instance;
            _method = method;
            _type = method.ReturnType;
            this.iRPC = iRPC;
            ////////////// Delegate ////////////////
            MakeDelegates(this.iRPC);
        }

        public RPCInvoker(MonoBehaviour instance, MethodInfo method, gRPCAttribute gRPC)
        {
            _instance = instance;
            _method = method;
            _type = method.ReturnType;
            this.gRPC = gRPC;
            ////////////// Delegate ////////////////
            MakeDelegates(this.gRPC);
        }

        private void MakeDelegates(Attribute attribute)
        {
            try
            {
                if (attribute is iRPCAttribute)
                {
                    if (_type == typeof(void))
                    {
                        iRPCVoid = (Action<NeutronStream.IReader, NeutronPlayer>)_method.CreateDelegate(typeof(Action<NeutronStream.IReader, NeutronPlayer>), _instance);
                        _methodType = MethodType.Void;
                    }
                    else if (_type == typeof(bool))
                    {
                        iRPCBool = (Func<NeutronStream.IReader, NeutronPlayer, bool>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, NeutronPlayer, bool>), _instance);
                        _methodType = MethodType.Bool;
                    }
                    else if (_type == typeof(Task))
                    {
                        iRPCTaskAsync = (Func<NeutronStream.IReader, NeutronPlayer, Task>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, NeutronPlayer, Task>), _instance);
                        _methodType = MethodType.Async | MethodType.Task;
                    }
                    else if (_type == typeof(Task<bool>))
                    {
                        iRPCBoolAsync = (Func<NeutronStream.IReader, NeutronPlayer, Task<bool>>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, NeutronPlayer, Task<bool>>), _instance);
                        _methodType = MethodType.Async | MethodType.Bool;
                    }
                }
                else if (attribute is gRPCAttribute)
                {
                    if (_type == typeof(void))
                    {
                        gRPCVoid = (Action<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron>)_method.CreateDelegate(typeof(Action<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron>), _instance);
                        _methodType = MethodType.Void;
                    }
                    else if (_type == typeof(int))
                    {
                        gRPCInt = (Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, int>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, int>), _instance);
                        _methodType = MethodType.Int;
                    }
                    else if (_type == typeof(bool))
                    {
                        gRPCBool = (Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, bool>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, bool>), _instance);
                        _methodType = MethodType.Bool;
                    }
                    else if (_type == typeof(Task))
                    {
                        gRPCTaskAsync = (Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task>), _instance);
                        _methodType = MethodType.Async | MethodType.Task;
                    }
                    else if (_type == typeof(Task<int>))
                    {
                        gRPCIntAsync = (Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<int>>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<int>>), _instance);
                        _methodType = MethodType.Async | MethodType.Int;
                    }
                    else if (_type == typeof(Task<bool>))
                    {
                        gRPCBoolAsync = (Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<bool>>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<bool>>), _instance);
                        _methodType = MethodType.Async | MethodType.Bool;
                    }
                    else if (_type == typeof(Task<NeutronView>))
                    {
                        gRPCViewAsync = (Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<NeutronView>>)_method.CreateDelegate(typeof(Func<NeutronStream.IReader, bool, bool, NeutronPlayer, Neutron, Task<NeutronView>>), _instance);
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