using System;
using UnityEngine;

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* Attribute to mark a variable or property as a auto-synchonized, which are synchronized from the server to clients and vice-versa.<br/>
    ///* This attribute allows validation of entries if the authority belongs to the client.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SyncVarAttribute : PropertyAttribute
    { }
}