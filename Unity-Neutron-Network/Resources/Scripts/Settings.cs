using System;

[Serializable]
public class NeutronGlobalSettings
{
    public string Address;
    public int Port;
    public Serialization Serialization;
    public Compression Compression;
    public bool NoDelay = true;
}

[Serializable]
public class NeutronServerSettings
{
    public int BackLog;
    public int FPS;
    public int MonoChunkSize;
    public int PacketChunkSize;
    public int ProcessChunkSize;
    public bool AntiCheat;
}

[Serializable]
public class NeutronClientSettings
{
    public int FPS;
    public int MonoChunkSize;
}

[Serializable]
public class NeutronPermissionsSettings
{

}

[Serializable]
public class NeutronHandleSettings
{

}