using NeutronNetwork.Internal.Server.Delegates;
using UnityEngine;

namespace NeutronNetwork.Helpers
{
    public class CheatsHelper
    {
        public static event Events.OnCheatDetected m_OnCheatDetected;
        public static bool m_isEnabled;

        public static bool Teleport(Vector3 lagDistance, float tolerance, Player detectedPlayer)
        {
            if (m_isEnabled)
            {
                if (lagDistance.magnitude > tolerance)
                {
                    return Notify(detectedPlayer, $"Teleport Detected T: {tolerance}");
                }
            }
            return false;
        }

        public static bool SpeedHack(float currentFrequency, float tolerance, Player detectedPlayer)
        {
            if (m_isEnabled)
            {
                if (currentFrequency > tolerance)
                {
                    return Notify(detectedPlayer, $"Speedhack Detected T: {tolerance}");
                }
            }
            return false;
        }

        private static bool Notify(Player detectedPlayer, string message)
        {
            m_OnCheatDetected?.Invoke(detectedPlayer, message);
            return true;
        }
    }
}