using NeutronNetwork.Packets;
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
    ///* É Usado para a comunicação geral, ex: Eventos, Criação de objetos.....etc.<br/>
    ///* É usado por instâncias globais, isto é, não pode existir em objetos de rede, funciona mas como um metódo estático/global.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable IDE1006
    public class gRPC : Attribute
#pragma warning restore IDE1006
    {
        public byte ID { get; set; }
        public bool FirstValidation { get; set; }
        public CacheMode Cache { get; set; }
        public TargetTo TargetTo { get; set; }
        public TunnelingTo TunnelingTo { get; set; }

        public gRPC()
        { }
    }
}