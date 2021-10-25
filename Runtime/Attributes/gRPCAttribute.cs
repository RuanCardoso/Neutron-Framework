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
    ///* É usado para a comunicação geral, ex: Eventos, Chat, Criação de objetos.....etc.<br/>
    ///* É usado por instâncias globais, isto é, funciona mas como um metódo estático/global.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable IDE1006
    public class gRPCAttribute : Attribute
#pragma warning restore IDE1006
    {
        /// <summary>
        ///* Id globalmente exclusivo para a identificação do metódo na rede.
        /// </summary>
        public byte ID
        {
            get;
            set;
        }

        /// <summary>
        ///* Defina como o metódo será "cachado" no servidor.
        /// </summary>
        public CacheMode Cache
        {
            get;
            set;
        } = CacheMode.None;

        /// <summary>
        ///* Defina para quem os dados devem ser redirecionados.
        /// </summary>
        public TargetTo TargetTo
        {
            get;
            set;
        } = TargetTo.Me;

        /// <summary>
        ///* Defina o túnel que será usado para redirecionar os dados.
        /// </summary>
        public TunnelingTo TunnelingTo
        {
            get;
            set;
        } = TunnelingTo.Me;

        public gRPCAttribute()
        { }
    }
}