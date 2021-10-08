using System;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    /// <summary>
    ///* É usado para a comunicação geral, ex: Movimento, Animações.....etc.<br/>
    ///* É usado por instância, isto é, os metódos são exclusivos por instância de script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable IDE1006
    public class iRPC : Attribute
#pragma warning restore IDE1006
    {
        /// <summary>
        ///* Id exclusivo por instância de script, usado para a identificação do metódo na rede.
        /// </summary>
        public byte ID {
            get;
            set;
        }

        public iRPC()
        { }
    }
}