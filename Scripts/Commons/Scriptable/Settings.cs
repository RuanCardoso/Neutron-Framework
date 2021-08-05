using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/Settings", fileName = "Neutron Settings")]
    public class Settings : ScriptableObject
    {
        public NeutronGlobalSettings GlobalSettings = new NeutronGlobalSettings();
        [HorizontalLine] public NeutronEditorSettings EditorSettings;
        [HorizontalLine] public NeutronClientSettings ClientSettings;
        [HorizontalLine] public NeutronServerSettings ServerSettings;
        [HorizontalLine] public NeutronLagSettings LagSimulationSettings;
        [HorizontalLine] public NeutronConstantsSettings NetworkSettings;

        [ContextMenu("Generate AppId")]
        public void NewGuid()
        {
#if UNITY_EDITOR
            GlobalSettings.AppId = Guid.NewGuid().ToString();
#endif
        }

        public void Reset()
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(GlobalSettings.AppId))
                NewGuid();
#endif
        }
    }
}