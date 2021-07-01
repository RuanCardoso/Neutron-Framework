using System;

namespace NeutronNetwork
{
    /// <summary>
    ///* É Usado para a comunicação geral, ex: Movimento, Animações.....etc.<br/>
    ///* É usado por instância, isto é, pode existir em vários objetos de rede(Jogador ou Objeto de Cena).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class iRPC : Attribute
    {
        //* O ID do iRPC, ele é usado para invocar o metódo.
        public int ID;
        //* Define se o metódo deve ser invocado na Main Thread(Unity).
        public bool DispatchOnMainThread;
        //* Se verdadeiro, o servidor invoca o metódo no lado do servidor e depois envia para os clientes, se falso, o servidor envia primeiro para depois invocar.
        public bool SendAfterProcessing;
        /// <summary>
        ///* É Usado para a comunicação geral, ex: Movimento, Animações.....etc.<br/>
        ///* É usado por instância, isto é, pode existir em vários objetos de rede(Jogador ou Objeto de Cena).
        /// </summary>
        /// <param name="ID">* ID do iRPC.</param>
        /// <param name="DispatchOnMainThread">* Define se o metódo é invocado no Thread Main(Unity)</param>
        /// <param name="SendAfterProcessing">* Se verdadeiro, o servidor invoca o metódo no seu lado e depois envia para os clientes, se falso, o servidor envia primeiro para depois invocar.</param>
        public iRPC(int ID, bool DispatchOnMainThread = false, bool SendAfterProcessing = true)
        {
            this.ID = ID;
            this.DispatchOnMainThread = DispatchOnMainThread;
            this.SendAfterProcessing = SendAfterProcessing;
        }
    }
}