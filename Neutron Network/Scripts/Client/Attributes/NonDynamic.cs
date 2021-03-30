using System;

/// <summary>
/// <para>PT: Ao contrário do Dynamic, este atributo é exatamente a mesma coisa, é usado para a comunicação entre servidor e clientes, a diferença é que este não necessita de um NeutronView.</para>
/// <para>EN: Unlike Dynamic, this attribute is exactly the same, it is used for communication between server and clients, the difference is that it does not need a NeutronView.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class NonDynamic : Attribute
{
    public int ID;
    /// <summary>
    /// <para>PT: ID Usado para invocar o metódo pela rede.</para>
    /// <para>EN: ID Used to invoke the method over the network.</para>
    /// </summary>
    /// <param name="ID">
    /// <para>PT: Ao contrário do Dynamic, este ID deve ser único, só pode existir um metódo com o mesmo ID.</para><br/>
    /// <para>EN: Unlike Dynamic, this ID must be unique, there can only be one method with the same ID.</para>
    /// </param>
    public NonDynamic(int ID)
    {
        this.ID = ID;
    }
}