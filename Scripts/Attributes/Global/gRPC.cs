using System;

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
        public bool RunInMonoBehaviour { get; set; }
        public bool RunInServer { get; set; }
        public bool SendAfterProcessing { get; set; }
        public Cache Cache { get; set; }
        public TargetTo TargetTo { get; set; }
        public TunnelingTo TunnelingTo { get; set; }
        public Protocol Protocol { get; set; }

        /// <summary>
        ///* É Usado para a comunicação geral, ex: Eventos, Criação de objetos.....etc.<br/>
        ///* É usado por instâncias globais, isto é, não pode existir em objetos de rede, funciona mas como um metódo estático/global.
        /// </summary>
        /// <param name="id">* O ID do gRPC.</param>
        /// <param name="runInMonoBehaviour">* Define se o metódo é invocado na Main Thread(Unity).</param>
        /// <param name="runInServer">* Define se o metódo é invocado no Servidor.</param>
        /// <param name="sendAfterProcessing">* Se verdadeiro, o servidor invoca o metódo no seu lado e depois envia para os clientes, se falso, o servidor envia primeiro para depois invocar.</param>
        /// <param name="cache">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="targetTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="tunnelingTo">* O Túnel que será usado para a transmissão.</param>
        /// <param name="protocol">* O protocolo que será usado para receber o gRPC.</param>
        public gRPC(byte id, bool runInMonoBehaviour = false, bool runInServer = true, bool sendAfterProcessing = false, Cache cache = Cache.None, TargetTo targetTo = TargetTo.Me, TunnelingTo tunnelingTo = TunnelingTo.Me, Protocol protocol = Protocol.Tcp)
        {
            ID = id;
            RunInMonoBehaviour = runInMonoBehaviour;
            RunInServer = runInServer;
            SendAfterProcessing = sendAfterProcessing;
            Cache = cache;
            TargetTo = targetTo;
            TunnelingTo = tunnelingTo;
            Protocol = protocol;
        }
    }
}