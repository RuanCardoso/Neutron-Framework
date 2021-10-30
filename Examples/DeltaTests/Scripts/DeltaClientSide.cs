namespace NeutronNetwork.Examples.DeltaEx
{
    public class DeltaClientSide : ClientSide
    {
        protected override void Start()
        {
            base.Start();
        }

        protected override void OnPlayerConnected(NeutronPlayer player, bool isMine, Neutron neutron)
        {
            base.OnPlayerConnected(player, isMine, neutron);
            {
                if (isMine)
                    neutron.JoinChannel(0);
            }
        }

        protected override void OnPlayerJoinedChannel(NeutronChannel channel, NeutronPlayer player, bool isMine, Neutron neutron)
        {
            base.OnPlayerJoinedChannel(channel, player, isMine, neutron);
        }
    }
}