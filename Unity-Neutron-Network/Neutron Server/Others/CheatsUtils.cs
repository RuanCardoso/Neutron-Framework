using NeutronNetwork.Internal.Server;
using UnityEngine;

namespace NeutronNetwork.Internal.Server.Cheats
{
    public class CheatsUtils
    {
        public static bool enabled = true;
        public static bool AntiTeleport(Vector3 oldPosition, Vector3 newPosition, float tolerance, Player detectedPlayer)
        {
            if (enabled)
            {
                if (Mathf.Abs(Vector3.Distance(oldPosition, newPosition)) > tolerance)
                {
                    NeutronSFunc.onCheatDetected(detectedPlayer, $"Teleport Detected T: {tolerance}");
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
                    NeutronSFunc.onCheatDetected(detectedPlayer, $"Speedhack Detected T: {tolerance}");
                    return true;
                }
            }
            return false;
        }
    }
}