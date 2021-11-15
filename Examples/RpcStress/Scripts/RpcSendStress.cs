using UnityEngine;

namespace NeutronNetwork.Examples.RpcStress
{
    public class RpcSendStress : NeutronBehaviour
    {
        protected override void Update()
        {
            base.Update();
            {
                if (HasAuthority)
                {
                    using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                    {
                        var writer = Begin_iRPC(10, stream, out var options);
                        writer.Write();
                        End_iRPC(10, stream);
                    }
                }
            }
        }

        [iRPC(10)]
        public bool RpcStress(NeutronStream.IReader reader, NeutronPlayer player)
        {
            Debug.LogError(IsServer);
            return true;
        }
    }
}