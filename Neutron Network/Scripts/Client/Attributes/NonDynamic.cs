using System;

/// <summary>
/// <para>PT: Ao contrário do Dynamic, este atributo é exatamente a mesma coisa, é usado para a comunicação entre servidor e clientes, a diferença é que este não necessita de um NeutronView, é global.</para>
/// <para>EN: Unlike Dynamic, this attribute is exactly the same, it is used for communication between server and clients, the difference is that it does not need a NeutronView, it's global.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class NonDynamic : Attribute
{
    public int ID;
    public bool DispatchOnMainThread;
    public bool SendAfterProcessing;
    public CacheMode cacheMode;
    public SendTo sendTo;
    public Broadcast broadcast;
    public Protocol protocol;
    /// <summary>
    /// <para>PT: ID Usado para invocar o metódo pela rede.</para>
    /// <para>EN: ID Used to invoke the method over the network.</para>
    /// </summary>
    public NonDynamic(int ID, bool DispatchOnMainThread = false, bool SendAfterProcessing = true, CacheMode cacheMode = CacheMode.None, SendTo sendTo = SendTo.Me, Broadcast broadcast = Broadcast.Me, Protocol protocol = Protocol.Tcp)
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