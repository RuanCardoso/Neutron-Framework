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
    ///* É Usado para a comunicação geral, ex: Movimento, Animações.....etc.<br/>
    ///* É usado por instância, isto é, pode existir em vários objetos de rede(Jogador ou Objeto de Cena).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
#pragma warning disable IDE1006
    public class iRPC : Attribute
#pragma warning restore IDE1006
    {
        public byte ID { get; set; }
        public bool FirstValidation { get; set; }

        public iRPC()
        { }
    }
}