using NeutronNetwork;

public static class Encapsulate
{
    public static object BeginLock = new object();
    public static NeutronPlayer Sender {
        get;
        set;
    }
}