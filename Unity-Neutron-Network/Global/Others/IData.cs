using NeutronNetwork.Internal.Cipher;
using Newtonsoft.Json;
using System;
using UnityEngine;

public class Data
{
    public const string PASS = "duG@%sATOTbL";
    public static JsonData LoadSettings() => JsonConvert.DeserializeObject<JsonData>(Resources.Load<TextAsset>("neutronsettings").text.Decrypt(PASS));
}

[Serializable]
public class JsonData
{
    public int serializationOptions { get; set; }
    public int compressionOptions { get; set; }
    public int serverPort { get; set; }
    public int voicePort { get; set; }
    public int backLog { get; set; }
    public int serverFPS { get; set; }
    public int serverMonoChunkSize { get; set; }
    public int serverPacketChunkSize { get; set; }
    public int serverProcessChunkSize { get; set; }
    public int serverSendRate { get; set; }
    public int serverSendRateUDP { get; set; }
    public int serverReceiveRate { get; set; }
    public int serverReceiveRateUDP { get; set; }
    public int clientReceiveRate { get; set; }
    public int clientReceiveRateUDP { get; set; }
    public int clientFPS { get; set; }
    public int clientMonoChunkSize { get; set; }
    public int clientSendRate { get; set; }
    public int clientSendRateUDP { get; set; }
    public bool serverNoDelay { get; set; }
    public bool clientNoDelay { get; set; }
    public bool antiCheat { get; set; }
    public bool dontDestroyOnLoad { get; set; }
    public bool UDPDontFragment { get; set; }
    public int speedHackTolerance { get; set; }
    public int teleportTolerance { get; set; }
    public int max_rec_msg { get; set; }
    public int max_send_msg { get; set; }
    public int limit_of_conn_by_ip { get; set; }
    public string ipAddress { get; set; }
}