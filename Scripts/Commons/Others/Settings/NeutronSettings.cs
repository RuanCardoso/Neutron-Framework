using NeutronNetwork.Constants;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    [CreateAssetMenu(menuName = "Neutron/Settings")]
    public class NeutronSettings : ScriptableObject
    {
        [Header("[Server & Client]")]
        public NeutronGlobalSettings GlobalSettings;

        [Header("[Editor]")]
        public NeutronEditorSettings EditorSettings;

        [Header("[Client]")]
        public NeutronClientSettings ClientSettings;

        [Header("[Server]")]
        public NeutronServerSettings ServerSettings;

        [Header("[Permissions]")]
        public NeutronPermissionsSettings PermissionsSettings;

        [Header("[Handles]")]
        public NeutronHandleSettings HandleSettings;

        [Header("[Constants]")]
        public int MAX_REC_MSG = 512;
        public int LIMIT_OF_CONN_BY_IP = 3;
    }
}