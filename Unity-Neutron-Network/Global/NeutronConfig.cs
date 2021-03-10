using UnityEngine;

public class NeutronConfig : MonoBehaviour
{
    public static JsonData GetConfig { get; private set; }
    public static Compression COMPRESSION_MODE;
    private void Awake()
    {
        LoadSettings();
    }

    public static void LoadSettings()
    {
        if (GetConfig == null)
            GetConfig = NeutronData.LoadSettings();
        COMPRESSION_MODE = (Compression)GetConfig.compressionOptions;
    }
}