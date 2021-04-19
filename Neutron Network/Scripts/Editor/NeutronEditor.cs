using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Cipher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Threading.Tasks;

public class NeutronEditor : EditorWindow
{
    [MenuItem("Neutron/Neutron/Settings")]
    private static void LocateSettings()
    {
        UnityEngine.Object asset = Resources.Load("Neutron Settings");
        if (asset != null)
            AssetDatabase.OpenAsset(asset);
    }
}