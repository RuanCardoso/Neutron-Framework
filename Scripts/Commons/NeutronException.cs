using System;
using System.Runtime.Serialization;

namespace NeutronNetwork.Internal
{
    public class NeutronException : Exception
    {
        public NeutronException()
        {
        }

        public NeutronException(String message) : base(message)
        {
        }

        public NeutronException(String message, Exception innerException) : base(message, innerException)
        {
        }

        protected NeutronException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}