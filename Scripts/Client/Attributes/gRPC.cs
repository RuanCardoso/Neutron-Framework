using System;

namespace NeutronNetwork
{
    /// <summary>
    ///* É Usado para a comunicação geral, ex: Eventos, Criação de objetos.....etc.<br/>
    ///* É usado por instâncias globais, isto é, não pode existir em objetos de rede, funciona mas como um metódo estático/global.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class gRPC : Attribute
    {
        //* O ID do gRPC, ele é usado para invocar o metódo.
        public int ID;
        //* Define se o metódo deve ser invocado na Main Thread(Unity).
        public bool DispatchOnMainThread;
        //* Se verdadeiro, o servidor invoca o metódo no lado do servidor e depois envia para os clientes, se falso, o servidor envia primeiro para depois invocar.
        public bool SendAfterProcessing;
        //* Define o modo de armazenamento em cache do gRPC.
        public CacheMode cacheMode;
        //* Define quais jogadores devem ser incluídos na lista de recepção do gRPC.
        public SendTo sendTo;
        //* O Túnel que será usado para a transmissão.
        public Broadcast broadcast;
        //* O protocolo que será usado para enviar o pacote.
        public Protocol protocol;

        /// <summary>
        ///* É Usado para a comunicação geral, ex: Eventos, Criação de objetos.....etc.<br/>
        ///* É usado por instâncias globais, isto é, não pode existir em objetos de rede, funciona mas como um metódo estático/global.
        /// </summary>
        /// <param name="ID">* O ID do gRPC.</param>
        /// <param name="DispatchOnMainThread">* Define se o metódo é invocado na Main Thread(Unity).</param>
        /// <param name="SendAfterProcessing">* Se verdadeiro, o servidor invoca o metódo no seu lado e depois envia para os clientes, se falso, o servidor envia primeiro para depois invocar.</param>
        /// <param name="cacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="sendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="broadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="protocol">* O protocolo que será usado para receber o gRPC.</param>
        public gRPC(int ID, bool DispatchOnMainThread = false, bool SendAfterProcessing = true, CacheMode cacheMode = CacheMode.None, SendTo sendTo = SendTo.Me, Broadcast broadcast = Broadcast.Me, Protocol protocol = Protocol.Tcp)
        {
            this.ID = ID;
            this.DispatchOnMainThread = DispatchOnMainThread;
            this.SendAfterProcessing = SendAfterProcessing;
            this.cacheMode = cacheMode;
            this.sendTo = sendTo;
            this.broadcast = broadcast;
            this.protocol = protocol;
        }
    }
}