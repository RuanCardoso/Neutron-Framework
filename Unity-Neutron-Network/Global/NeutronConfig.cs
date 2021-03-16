using UnityEngine;

public class NeutronConfig : MonoBehaviour
{
    public static NeutronSettings Settings { get; private set; }
    private void Awake()
    {
        LoadSettings();
    }

    public static void LoadSettings()
    {
        if (Settings == null)
            Settings = Resources.Load<NeutronSettings>("Neutron Settings");
    }
}