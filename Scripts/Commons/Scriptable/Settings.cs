using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/Settings", fileName = "Neutron Settings")]
    public class Settings : ScriptableObject
    {
        public NGlobalSettings GlobalSettings;
        [HorizontalLine] public NEditorSettings EditorSettings;
        [HorizontalLine] public NClientSettings ClientSettings;
        [HorizontalLine] public NServerSettings ServerSettings;
        [HorizontalLine] public NLagSettings LagSimulationSettings;
        [BoxGroup("Constants")] public int MAX_REC_MSG = 512;
        [BoxGroup("Constants")] public int LIMIT_OF_CONN_BY_IP = 3;
    }
}