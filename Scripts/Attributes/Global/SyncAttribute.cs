using System;
using UnityEngine;

namespace NeutronNetwork
{
    /// <summary>
    ///* Define se um campo é serializado e sincronizado via rede.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SyncAttribute : PropertyAttribute
    { }
}