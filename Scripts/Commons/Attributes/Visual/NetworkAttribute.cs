using System;

namespace NeutronNetwork.Internal.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class NetworkAttribute : Attribute
    {
        public NetworkAttribute()
        {
        }

        public NetworkAttribute(string log)
        {
        }
    }
}