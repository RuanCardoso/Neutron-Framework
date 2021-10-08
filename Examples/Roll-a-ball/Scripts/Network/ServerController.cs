using NeutronNetwork;
using NeutronNetwork.Packets;
using System.Threading.Tasks;
using UnityEngine;

public class ServerController : ServerSide
{
    [SerializeField] private GameObject playerPrefab;
    protected override void OnPlayerJoinedChannel(NeutronPlayer player)
    {
        base.OnPlayerJoinedChannel(player);
        {
            using (NeutronStream stream = new NeutronStream())
            {
                var writer = Instance.BeginPlayer(stream, Vector3.zero, Quaternion.identity);
                writer.Write((ushort)54634);
                writer.Write();
                Instance.EndPlayer(stream, 9, player);
            }
        }
    }

    [gRPC(ID = 9, TargetTo = TargetTo.All, TunnelingTo = TunnelingTo.Channel, Cache = CacheMode.Overwrite)]
    public async void InstantiatePlayer(NeutronStream.IReader reader, bool isServer, bool isMine, NeutronPlayer player, Neutron neutron)
    {
        if (neutron.EndPlayer(reader, out Vector3 pos, out Quaternion rot))
        {
            await NeutronSchedule.ScheduleTaskAsync(() =>
            {
                var nV = Neutron.NetworkSpawn(reader, isServer, player, playerPrefab, pos, rot, neutron);
                if (isServer)
                    nV.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);
                return nV;
            });
        }
        else
            LogHelper.Error("Failed to instantiate player");
    }
}