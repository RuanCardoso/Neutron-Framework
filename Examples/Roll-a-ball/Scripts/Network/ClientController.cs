using NeutronNetwork;
using NeutronNetwork.Packets;

public class ClientController : ClientSide
{
    protected override bool AutoStartConnection => base.AutoStartConnection;
    protected override int VirtualPlayerCount => base.VirtualPlayerCount;

    protected override void OnNeutronConnected(System.Boolean isSuccess, Neutron neutron)
    {
        if (isSuccess)
            LogHelper.Info("Player connected with successful.");
        else
            LogHelper.Error("Player connected with error!");
    }

    protected override void OnPlayerConnected(NeutronPlayer player, System.Boolean isMine, Neutron neutron)
    {
        if (isMine)
            neutron.JoinChannel(0);
    }

    protected async override void OnPlayerJoinedChannel(NeutronChannel channel, NeutronPlayer player, System.Boolean isMine, Neutron neutron)
    {
        if (isMine)
        {
            await neutron.Synchronize();
            LogHelper.Error($"Player joined in channel {channel.Name}");
            neutron.GetCache(CachedPacket.gRPC, 0, false);
        }
    }
}