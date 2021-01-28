using Newtonsoft.Json;
using System;
using UnityEngine;

public class Data
{
    public static IData LoadSettings()
    {
        try
        {
            TextAsset fromJson = Resources.Load<TextAsset>("neutronsettings");
            IData data = JsonConvert.DeserializeObject<IData>(fromJson.text);
            return data;
        }
        catch (Exception ex) { Utils.LoggerError("Json: " + ex.Message); }
        return null;
    }
}

public class IData
{
    public Compression compressionOptions { get; set; }
    public int serverPort { get; set; }
    public int voicePort { get; set; }
    public int backLog { get; set; }
    public int FPS { get; set; }
    public int DPF { get; set; }
    public int sendRate { get; set; }
    public bool quickPackets { get; set; }
    public bool noDelay { get; set; }
    public bool antiCheat { get; set; }
    public bool dontDestroyOnLoad { get; set; }
    public int speedHackTolerance { get; set; }
    public int teleportTolerance { get; set; }
    public string loginUri { get; set; }
}