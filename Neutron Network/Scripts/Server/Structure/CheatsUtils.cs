using NeutronNetwork.Internal.Server.InternalEvents;
using UnityEngine;

namespace NeutronNetwork.Internal.Server.Cheats
{
    public class CheatsUtils
    {
        public static event ServerEvents.OnCheatDetected onCheatDetected;
        public static bool enabled = true;

        public static bool Teleport(Vector3 lagDistance, float tolerance, Player detectedPlayer)
        {
            if (enabled)
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
            if (enabled)
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
            onCheatDetected?.Invoke(detectedPlayer, message);
            return true;
        }
    }
}