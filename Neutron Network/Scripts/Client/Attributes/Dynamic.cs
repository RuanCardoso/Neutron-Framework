using System;

/// <summary>
/// <para>PT: Este atributo é usado para a comunicação entre servidor e clientes, este attributo só pode ser usado em objetos que possuem o componente NeutronView.</para>
/// <para>EN: This attribute is used for communication between server and clients, this attribute can only be used on objects that have the NeutronView component.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class Dynamic : Attribute
{
    public int ID;
    public bool DispatchOnMainThread;
    public bool SendAfterProcessing;
    /// <summary>
    /// <para>PT: Marca o metódo com um ID, este ID será usado para invocar o metódo entre os clientes e servidor.</para>
    /// <para>EN: Marks the method with an ID, this ID will be used to invoke the method between the clients and the server.</para>
    /// </summary>
    public Dynamic(int ID, bool DispatchOnMainThread = false, bool SendAfterProcessing = true)
    {
        this.ID = ID;
        this.DispatchOnMainThread = DispatchOnMainThread;
        this.SendAfterProcessing = SendAfterProcessing;
    }
}