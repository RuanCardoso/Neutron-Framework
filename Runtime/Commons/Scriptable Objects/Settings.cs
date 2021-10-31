using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/Settings", fileName = "Neutron Settings")]
    public class Settings : ScriptableObject
    {
        [InfoBox("Performance in Unity Editor is low!", EInfoBoxType.Warning)]
        public NeutronGlobalSettings GlobalSettings = new NeutronGlobalSettings();
        [HorizontalLine] public NeutronClientSettings ClientSettings;
        [HorizontalLine] public NeutronServerSettings ServerSettings;
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

        private void OnValidate()
        {
#if UNITY_EDITOR
            for (int i = 0; i < GlobalSettings.Addresses.Length; i++)
            {
                GlobalSettings.Addresses[i] = GlobalSettings.Addresses[i].Replace(" ", "");
            }
#endif
        }
    }
}