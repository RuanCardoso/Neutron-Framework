using System.IO;
using UnityEngine;

public class NeutronWriter : BinaryWriter {
    public NeutronWriter () : base (new MemoryStream ()) { }

    public NeutronWriter (MemoryStream newStream) : base (newStream) { }

    public void Write (Color writable) {
        Write (writable.r);
        Write (writable.g);
        Write (writable.b);
        Write (writable.a);
    }

    public void Write (Vector2 writable) {
        Write (writable.x);
        Write (writable.y);
    }

    public void Write (Vector3 writable) {
        Write (writable.x);
        Write (writable.y);
        Write (writable.z);
    }

    public void Write (SerializableVector3 writable) {
        Write (writable.x);
        Write (writable.y);
        Write (writable.z);
    }

    public void Write (Quaternion writable) {
        Write (writable.x);
        Write (writable.y);
        Write (writable.z);
        Write (writable.w);
    }

    public void Write (Key writable) {
        Write (writable.key.ToString ());
        Write (writable.value.ToString ());
    }

    public void WritePacket<T> (T packet) {
        Write ((int) (object) packet);
    }

    public byte[] ToArray () {
        return ((MemoryStream) base.BaseStream).ToArray ();
    }

    public new void Dispose () {
        base.Close ();
        base.BaseStream.Close ();
    }
}

public class NeutronReader : BinaryReader {
    public NeutronReader (byte[] buffer) : base (new MemoryStream (buffer)) { }
    public NeutronReader (byte[] buffer, int index, int count) : base(new MemoryStream(buffer, index, count)) { }

    public NeutronReader (MemoryStream newStream) : base (newStream) { }

    public Color ReadColor () {
        float r = ReadSingle ();
        float g = ReadSingle ();
        float b = ReadSingle ();
        float a = ReadSingle ();
        return new Color (r, g, b, a);
    }

    public Vector2 ReadVector2 () {
        float x = ReadSingle ();
        float y = ReadSingle ();
        return new Vector2 (x, y);
    }

    public Vector3 ReadVector3 () {
        float x = ReadSingle ();
        float y = ReadSingle ();
        float z = ReadSingle ();
        return new Vector3 (x, y, z);
    }

    public SerializableVector3 ReadSerializableVector3 () {
        float x = ReadSingle ();
        float y = ReadSingle ();
        float z = ReadSingle ();
        return new SerializableVector3 (x, y, z);
    }

    public Quaternion ReadQuaternion () {
        float x = ReadSingle ();
        float y = ReadSingle ();
        float z = ReadSingle ();
        float w = ReadSingle ();
        return new Quaternion (x, y, z, w);
    }

    public Key ReadKey () {
        string key = ReadString ();
        string value = ReadString ();
        return new Key (key, value);
    }

    public T ReadPacket<T> () {
        int packetID = ReadInt32 ();
        return (T) (object) packetID;
    }

    public byte[] ToArray()
    {
        return ((MemoryStream)base.BaseStream).ToArray();
    }

    public new void Dispose () {
        base.Close ();
        base.BaseStream.Close ();
    }
}