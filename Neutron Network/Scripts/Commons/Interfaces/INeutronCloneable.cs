using System;

public interface INeutronCloneable : ICloneable
{
    bool Equals(object other);
    int GetHashCode();
}