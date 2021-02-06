using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class Response : Attribute
{
    public int ID;

    public Response(int ID)
    {
        this.ID = ID;
    }
}