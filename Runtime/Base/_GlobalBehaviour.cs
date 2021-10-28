using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* Fornece suporte a operações gRPC.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONNECTION)]
    public class GlobalBehaviour : MonoBehaviour
    {
        #region Collections
        /// <summary>
        ///* Cache all methods marked with the gRPC attribute.
        /// </summary>
        /// <value></value>
#pragma warning disable IDE1006
        public static Dictionary<byte, RPCInvoker> gRPCs
        {
#pragma warning restore IDE1006
            get;
        } = new Dictionary<byte, RPCInvoker>();
        #endregion

        #region Properties
        /// <summary>
        ///* Return the neutron server instance.
        /// </summary>
        private Neutron Server => Neutron.Server.Instance;
        #endregion

        #region Mono Behaviour
        /// <summary>
        ///* Override the Unity Awake method.<br/>
        ///* Call the base.Awake();
        /// </summary>
        protected virtual void Awake() => MakeAttributes();
        #endregion

        #region Neutron
        /// <summary>
        ///* Initiates a gRPC(Global Remote Procedure Call) service call.<br/>
        ///* (from client-side) use this overload to send from client to server.
        /// </summary>
        /// <param name="parameters">The parameters that will be sent with the service.</param>
        /// <param name="neutron">The neutron instance that will be responsible for starting the service.</param>
        /// <returns></returns>
        public NeutronStream.IWriter Begin_gRPC(NeutronStream parameters, Neutron neutron) => neutron.Begin_gRPC(parameters);

        /// <summary>
        ///* Initiates a gRPC(Global Remote Procedure Call) service call.<br/>
        ///* (from server-side) use this overload to send from server to client.
        /// </summary>
        /// <param name="parameters">The parameters that will be sent with the service.</param>
        /// <returns></returns>
        public NeutronStream.IWriter Begin_gRPC(NeutronStream parameters) => Server.Begin_gRPC(parameters);

        /// <summary>
        ///* Ends a gRPC(Global Remote Procedure Call) service call.<br/>
        ///* (from client-side) use this overload to send from client to server.
        /// </summary>
        /// <param name="id">The id of the gRPC service.</param>
        /// <param name="parameters">The parameters that will be sent with the service.</param>
        /// <param name="protocol">The protocol used to send the service.</param>
        /// <param name="neutron">The neutron instance that will be responsible for ending the service.</param>
        public void End_gRPC(byte id, NeutronStream parameters, Protocol protocol, Neutron neutron)
        {
            neutron.End_gRPC(id, parameters, protocol);
        }

        /// <summary>
        ///* Ends a gRPC(Global Remote Procedure Call) service call.<br/>
        ///* (from server-side) use this overload to send from server to client.
        /// </summary>
        /// <param name="id">The id of the gRPC service.</param>
        /// <param name="parameters">The parameters that will be sent with the service.</param>
        /// <param name="protocol">The protocol used to send the service.</param>
        /// <param name="player">The player that will send the service.</param>
        public void End_gRPC(byte id, NeutronStream parameters, Protocol protocol, NeutronPlayer player)
        {
            Server.End_gRPC(id, parameters, protocol, player);
        }
        #endregion

        #region Reflection
        /// <summary>
        ///* Cache all methods marked with the gRPC attribute.
        /// </summary>
        private void MakeAttributes()
        {
            GlobalBehaviour localInstance = this; //* Local instance to avoid null reference.
            var instances = FindObjectsOfType<GlobalBehaviour>(); //* Find all instances of this class.
            Type localType = localInstance.GetType(); //* Local type to avoid null reference.
            (gRPCAttribute[], MethodInfo)[] ___ = ReflectionHelper.GetMultipleAttributesWithMethod<gRPCAttribute>(localInstance); //* Get all methods marked with the gRPC attribute.
            if ((localType.BaseType == typeof(PlayerGlobalController) || localType.BaseType == typeof(NeutronBehaviour)) && ___.Length > 0) //* If the current instance is a player controller or a neutron controller, then...
                throw new NeutronException($"The class \"{localType.Name}\", they cannot declare \"[gRPC]\" methods, but they can invoke it, to solve this problem you must declare the method in a globally unique script, for example, \"ClientController\" or \"ServerController\"");
            if (instances[instances.Length - 1] == localInstance)
            {
                //* If the current instance is the last instance of this class, then...
                for (int gI = 0; gI < instances.Length; gI++)
                {
                    GlobalBehaviour instance = instances[gI]; //* Local instance to avoid null reference.
                    if (instance != null && instance.enabled)
                    {
                        //* If the instance is not null and is enabled, then...
                        Type type = instance.GetType(); //* Local type to avoid null reference.
                        (gRPCAttribute[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<gRPCAttribute>(instance); //* Get all methods marked with the gRPC attribute.
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            //* For each method marked with the gRPC attribute...
                            (gRPCAttribute[], MethodInfo) methods = multiplesMethods[i]; //* Local methods to avoid null reference.
                            for (int ii = 0; ii < methods.Item1.Count(); ii++)
                            {
                                //* For each gRPC attribute...
                                gRPCAttribute method = methods.Item1[ii]; //* Local gRPC attribute to avoid null reference.
                                if (!gRPCs.ContainsKey(method.Id)) //* Check if the method is already registered.
                                    gRPCs.Add(method.Id, new RPCInvoker(instance, methods.Item2, method)); //* Register the method.
                                else
                                    throw new NeutronException($"gRPC: Duplicate Id not allowed in \"{type.Name}\" Id -> [{method.Id}]"); //* Throw an exception if the method is already registered.
                            }
                        }
                    }
                    else
                        continue; //* If the instance is null or is not enabled, then continue.
                }
            }
            else { } //* If the current instance is not the last instance of this class, then...
        }
        #endregion
    }
}