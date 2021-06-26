using System;

namespace NeutronNetwork
{
    /// <summary>
    /// <para>PT: Este atributo é usado para a comunicação entre servidor e clientes, este attributo só pode ser usado em objetos que possuem o componente NeutronView.</para>
    /// <para>EN: This attribute is used for communication between server and clients, this attribute can only be used on objects that have the NeutronView component.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class iRPC : Attribute
    {
        public int ID;
        public bool DispatchOnMainThread;
        public bool SendAfterProcessing;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="DispatchOnMainThread"></param>
        /// <param name="SendAfterProcessing"></param>
        public iRPC(int ID, bool DispatchOnMainThread = false, bool SendAfterProcessing = true)
        {
            this.ID = ID;
            this.DispatchOnMainThread = DispatchOnMainThread;
            this.SendAfterProcessing = SendAfterProcessing;
        }
    }
}