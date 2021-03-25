using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class Dynamic : Attribute
{
    public int ID;
    public Dynamic(int ID)
    {
        this.ID = ID;
    }
}