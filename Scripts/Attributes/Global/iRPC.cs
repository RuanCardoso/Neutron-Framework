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
        public bool RunInMonoBehaviour { get; set; }
        public bool RunInServer { get; set; }
        public bool SendAfterProcessing { get; set; }

        /// <summary>
        ///* É Usado para a comunicação geral, ex: Movimento, Animações, Gatilhos..etc.<br/>
        ///* É usado por instância, isto é, pode existir em vários objetos de rede(Jogador ou Objeto de Cena).
        /// </summary>
        /// <param name="id">* ID do iRPC.</param>
        /// <param name="runInMonoBehaviour">* Define se o metódo é invocado no Thread Main(Unity).</param>
        /// <param name="runInServer">* Define se o metódo é invocado no Servidor.</param>
        /// <param name="sendAfterProcessing">* Se verdadeiro, o servidor invoca o metódo no seu lado e depois envia para os clientes, se falso, o servidor envia primeiro para depois invocar.</param>
        public iRPC(byte id, bool runInMonoBehaviour = false, bool runInServer = true, bool sendAfterProcessing = false)
        {
            ID = id;
            RunInMonoBehaviour = runInMonoBehaviour;
            RunInServer = runInServer;
            SendAfterProcessing = sendAfterProcessing;
        }
    }
}