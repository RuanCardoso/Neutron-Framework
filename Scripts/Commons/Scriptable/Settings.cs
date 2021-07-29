using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using UnityEngine;

namespace NeutronNetwork
{
    [CreateAssetMenu(menuName = "Neutron/Settings", fileName = "Neutron Settings")]
    public class Settings : ScriptableObject
    {
        public NeutronGlobalSettings GlobalSettings;
        [HorizontalLine] public NeutronEditorSettings EditorSettings;
        [HorizontalLine] public NeutronClientSettings ClientSettings;
        [HorizontalLine] public NeutronServerSettings ServerSettings;
        [HorizontalLine] public NeutronLagSettings LagSimulationSettings;

        #region String's
        [BoxGroup("Constants")] public string CONTAINER_PLAYER_NAME = "[Container] -> Player[Main]";
        #endregion

        #region Integers
        [Range(1, 1400)]
        [BoxGroup("Constants")] public int MAX_MSG_UDP = 512;
        [BoxGroup("Constants")] public int MAX_MSG_TCP = 512;
        [Range(1, 5)]
        [BoxGroup("Constants")] public int LIMIT_OF_CONN_BY_IP = 3;
        [BoxGroup("Constants")] public int MAX_LATENCY = 150;
        public const int GENERATE_PLAYER_ID = 27716848;
        public const int BOUNDED_CAPACITY = int.MaxValue;
        public const int MIN_SEND_RATE = 1;
        public const int MAX_SEND_RATE = 128;
        #endregion

        #region Single's
        public const float ONE_PER_SECOND = 1F;
        #endregion

        #region Double's
        [BoxGroup("Constants")] public double NET_TIME_DESYNC_TOLERANCE = 1D;
        [BoxGroup("Constants")] public double RESYNC_TOLERANCE = 0.001D;
        #endregion

        #region Byte's
        public const byte CREATE_PLAYER = 255;
        public const byte CREATE_OBJECT = 254;
        #endregion
    }
}