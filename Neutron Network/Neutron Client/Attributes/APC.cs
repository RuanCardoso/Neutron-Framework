using System;

[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
public class APC : Attribute {
    public int ID;

    public APC (int ID) {
        this.ID = ID;
    }
}