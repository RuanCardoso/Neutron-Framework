using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ACC : Attribute
{
    public int ID;

    public ACC(int ID)
    {
        this.ID = ID;
    }
}