using UnityEngine;

namespace NeutronNetwork.Examples.SyncTransform
{
    public class SyncTransformClientSide : ClientSide
    {
        [SerializeField] private GameObject _player;
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
            {
                if (isMine)
                {
                    using (NeutronStream stream = new NeutronStream())
                    {
                        var writer = neutron.BeginPlayer(stream, Vector3.zero, Quaternion.identity);
                        writer.Write();
                        neutron.EndPlayer(stream, 10);
                    }
                }
            }
        }

        [gRPC(10, Packets.CacheMode.Overwrite, Packets.TargetTo.All, Packets.MatchmakingTo.Auto)]
        private bool OnCreatePlayer(NeutronStream.IReader reader, bool isServer, bool isMine, NeutronPlayer player, Neutron instance)
        {
            if (instance.EndPlayer(reader, out var pos, out var rot))
            {
                NeutronSchedule.ScheduleTask(() =>
                {
                    Neutron.NetworkSpawn(isServer, false, player, _player, pos, rot, instance);
                });
            }
            else
                LogHelper.Error("Failed to spawn player");
            return false;
        }
    }
}