using NeutronNetwork.Internal.Server.InternalEvents;
using UnityEngine;

namespace NeutronNetwork.Internal.Server.Cheats
{
    public class CheatsUtils
    {
        public static SEvents.OnCheatDetected onCheatDetected;
        public static bool enabled = true;
        public static bool AntiTeleport(Vector3 oldPosition, Vector3 newPosition, float tolerance, Player detectedPlayer)
        {
            if (enabled)
            {
                if (Mathf.Abs(Vector3.Distance(oldPosition, newPosition)) > tolerance)
                {
                    onCheatDetected(detectedPlayer, $"Teleport Detected T: {tolerance}");
                    return true;
                }
            }
            return false;
        }

        public static bool AntiSpeedHack(float currentFrequency, float tolerance, Player detectedPlayer)
        {
            if (enabled)
            {
                if (currentFrequency > tolerance)
                {
                    onCheatDetected(detectedPlayer, $"Speedhack Detected T: {tolerance}");
                    return true;
                }
            }
            return false;
        }
    }
}