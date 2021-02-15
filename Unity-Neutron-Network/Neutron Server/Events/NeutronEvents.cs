using UnityEngine;

namespace NeutronNetwork.Internal.Server.InternalEvents
{
    public class NeutronEvents : MonoBehaviour
    {
        public void Initialize()
        {
            NeutronServer.onServerAwake += OnServerAwake;
        }

        private void OnServerAwake()
        {
            
        }
    }
}