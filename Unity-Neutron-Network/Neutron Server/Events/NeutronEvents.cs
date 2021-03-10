using System;
using NeutronNetwork.Internal.Server.Cheats;
using UnityEngine;

namespace NeutronNetwork.Internal.Server.InternalEvents
{
    public class NeutronEvents : MonoBehaviour
    {
        public void Initialize()
        {
            NeutronServer.onServerAwake += OnServerAwake;
            CheatsUtils.onCheatDetected += OnCheatDetected;
        }

        private void OnCheatDetected(Player playerDetected, string cheatName)
        {
            Utilities.Logger($"Hm detectei alguem safado -> {playerDetected.Nickname}");
        }

        private void OnServerAwake()
        {

        }
    }
}