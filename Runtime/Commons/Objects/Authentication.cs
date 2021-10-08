using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Interfaces;
using System;
using System.Runtime.Serialization;

namespace NeutronNetwork
{
    [Serializable]
    public class Authentication : INeutronSerializable
    {
        public static Authentication Auth { get; } = new Authentication("Neutron", "Neutron");
        public string User { get; }
        public string Pass { get; }

        public Authentication()
        {
        }

        /// <summary>
        ///* Valores a serem autenticados.
        /// </summary>
        /// <param name="user">* O usuário, email ou qualquer outro tipo de Id Login.</param>
        /// <param name="pass">* A senha para autenticar o usuário.</param>
        public Authentication(string user, string pass, bool encrypt = true)
        {
            User = user;
            Pass = encrypt ? pass.Encrypt() : pass;
        }

        public Authentication(SerializationInfo info, StreamingContext context)
        {
            User = info.GetString("user");
            Pass = info.GetString("pass");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("user", User);
            info.AddValue("pass", Pass);
        }
    }
}