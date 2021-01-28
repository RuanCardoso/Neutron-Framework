using System;

[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
public class RPC : Attribute {
    public int ID;

    public RPC (int ID) {
        this.ID = ID;
    }
}