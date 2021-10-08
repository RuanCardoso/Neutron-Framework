using System;

namespace NeutronNetwork.Internal.Attributes
{
    [AttributeUsage(AttributeTargets.All)]
    public class ThreadSafeAttribute : Attribute
    {
        public ThreadSafeAttribute()
        {
        }

        public ThreadSafeAttribute(string log)
        {
        }
    }
}