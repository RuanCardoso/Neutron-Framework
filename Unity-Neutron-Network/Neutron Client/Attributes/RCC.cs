using System;

[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
public class RCC : Attribute {
    public int ID;

    public RCC (int ID) {
        this.ID = ID;
    }
}