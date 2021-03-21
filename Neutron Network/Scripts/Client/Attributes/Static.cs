using System;

[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
public class Static : Attribute {
    public int ID;

    public Static (int ID) {
        this.ID = ID;
    }
}