using UnityEngine;

[DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_CONFIG_ORDER)]
public class NeutronConfig : MonoBehaviour
{
    public static NeutronSettings Settings { get; private set; }
    private void Awake()
    {
        LoadSettings();
    }

    private void Start() { }

    private void LoadSettings()
    {
        if (Settings == null)
            Settings = Resources.Load<NeutronSettings>("Neutron Settings");
    }
}