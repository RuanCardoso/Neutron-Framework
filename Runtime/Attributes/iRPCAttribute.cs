using System;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* Attribute to mark a method as a iRPC(Instance Remote Procedure Call) service, the iRPC service must have a unique id in the same script instance.<br/>
    ///* The iRPC service allow use the same id on the different script instance.<br/>
    ///* In iRPC, a client or server application can directly call a method on a server or client application on a different or local machine as if it were a local object, making it easier for you to create distributed applications and services.<br/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable IDE1006
    public class iRPCAttribute : Attribute
#pragma warning restore IDE1006
    {
        /// <summary>
        ///* The unique id of the iRPC service.
        /// </summary>
        /// <value></value>
        public byte Id
        {
            get;
        }

        /// <summary>
        ///* Attribute to mark a method as a iRPC(Instance Remote Procedure Call) service, the iRPC service must have a unique id in the same script instance.<br/>
        ///* The iRPC service allow use the same id on the different script instance.<br/>
        ///* In iRPC, a client or server application can directly call a method on a server or client application on a different or local machine as if it were a local object, making it easier for you to create distributed applications and services.<br/>
        /// </summary>
        /// <param name="id">The unique id of the iRPC service.</param>
        public iRPCAttribute(byte id)
        {
            Id = id;
        }
    }
}