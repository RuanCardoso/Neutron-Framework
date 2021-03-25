using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class NonDynamic : Attribute
{
    public int ID;
    public NonDynamic(int ID)
    {
        this.ID = ID;
    }
}