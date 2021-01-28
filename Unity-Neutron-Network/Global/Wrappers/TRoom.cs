using System;

public class TRoom : Tuple<Room, NeutronReader>
{
    public Room room => Item1;
    public NeutronReader option => Item2;
    public TRoom(Room r, NeutronReader o) : base(r, o)
    { }
}