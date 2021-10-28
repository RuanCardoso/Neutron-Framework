using System;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork.Internal.Attributes
{
    /// <summary>
    ///* This attribute is used to mark a method as thread safe.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ThreadSafeAttribute : Attribute
    {
        /// <summary>
        ///* This attribute is used to mark a method as thread safe.
        /// </summary>
        public ThreadSafeAttribute()
        {
        }

        /// <summary>
        ///* This attribute is used to mark a method as thread safe.
        /// </summary>
        public ThreadSafeAttribute(string log)
        {
        }
    }
}