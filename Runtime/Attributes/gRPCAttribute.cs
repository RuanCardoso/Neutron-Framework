using NeutronNetwork.Packets;
using System;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* Attribute to mark a method as a gRPC(Global Remote Procedure Call) service, the gRPC service must have a globally unique Id and cannot be declared in scripts that have multiple instances.<br/>
    ///*In gRPC, a client or server application can directly call a method on a server or client application on a different or local machine as if it were a local object, making it easier for you to create distributed applications and services.<br/>
    ///* gRPC is based around the idea of defining a service, specifying the methods that can be called remotely with their parameters types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable IDE1006
    public class gRPCAttribute : Attribute
#pragma warning restore IDE1006
    {
        /// <summary>
        ///* The globally unique Id of the gRPC service.
        /// </summary>
        /// <value></value>
        public byte Id
        {
            get;
        }

        /// <summary>
        ///* Defines how the service will be cached on the server side.
        /// </summary>
        /// <value></value>
        public CacheMode CacheMode
        {
            get;
        }

        /// <summary>
        ///* These define which remote clients get your RPC call.
        /// </summary>
        /// <value></value>
        public TargetTo TargetTo
        {
            get;
        }

        /// <summary>
        ///* These define which matchmaking get your RPC call.
        /// </summary>
        public MatchmakingTo MatchmakingTo
        {
            get;
        }

        public gRPCAttribute(byte id, CacheMode cacheMode = CacheMode.None, TargetTo targetTo = TargetTo.All, MatchmakingTo matchmakingTo = MatchmakingTo.Auto)
        {
            Id = id;
            CacheMode = cacheMode;
            TargetTo = targetTo;
            MatchmakingTo = matchmakingTo;
        }
    }
}