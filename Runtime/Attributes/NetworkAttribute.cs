using System;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork.Internal.Attributes
{
    /// <summary>
    ///* Indicates that the property will be serialized and sent over the network.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class NetworkAttribute : Attribute
    {
        /// <summary>
        ///* Indicates that the property or method will be serialized and sent over the network.
        /// </summary>
        public NetworkAttribute()
        {
        }

        /// <summary>
        ///* Indicates that the property will be serialized and sent over the network.
        /// </summary>
        /// <param name="log">A reminder for something.</param>
        public NetworkAttribute(string log)
        {
        }

        /// <summary>
        ///* Indicates that the property will be serialized and sent over the network.
        /// </summary>
        /// <param name="size">The number of bytes the method or property will send over the network.</param>
        public NetworkAttribute(int size)
        {
        }
    }
}