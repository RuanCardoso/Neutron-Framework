using Fossil;
using NeutronNetwork.Constants;
using UnityEngine;

namespace NeutronNetwork.Examples.DeltaEx
{
    public class RpcAndSync : NeutronBehaviour
    {
        public float f = 100;
        private byte[] _originalData;
        private byte[] _targetData;
        private NeutronStream _stream = new NeutronStream();
        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            {
                int capacity = (sizeof(int) * 4) + PacketSize.iRPC;
                _originalData = new byte[] { 0x1, 0x1, 0x1, 0x1, 0x1 };
                _targetData = new byte[] { 0x1, 0x1, 0x1, 0x1, 0x3 };
            }
        }

        protected override void OnNeutronUpdate()
        {
            base.OnNeutronUpdate();
            {
                if (HasAuthority)
                {
                    for (int i = 0; i < f; i++)
                    {
                        var writer = Begin_iRPC(1, _stream, out var _);
                        using (NeutronStream deltaStream = Neutron.PooledNetworkStreams.Pull())
                        {
                            var deltaWriter = deltaStream.Writer;
                            deltaWriter.Write(128);
                            deltaWriter.Write(100);
                            deltaWriter.Write(1000);
                            deltaWriter.Write(10000);

                            //_originalData = deltaWriter.ToArray();
                            byte[] delta = Delta.Create(_originalData, _targetData);
                            //_targetData = _originalData;
                            writer.WriteNext(delta);
                            writer.Write();
                            End_iRPC(1, _stream);
                        }
                    }
                }
            }
        }

        [iRPC(3)]
        private void Mov(NeutronStream.IReader reader, NeutronPlayer player)
        {

        }

        [iRPC(1)]
        private void RpcTest(NeutronStream.IReader reader, NeutronPlayer player)
        {
            byte[] buffer = reader.ReadNext();
            using (NeutronStream deltaStream = Neutron.PooledNetworkStreams.Pull())
            {
                //byte[] applied = Delta.Apply(_originalData, buffer);
                //var deltaReader = deltaStream.Reader;
                //deltaReader.SetBuffer(applied);
                //if (IsClient)
                //    LogHelper.Error($"{IsServer} -> received");
                // Debug.LogError(deltaReader.ReadInt());
                // Debug.LogError(deltaReader.ReadInt());
                // Debug.LogError(deltaReader.ReadInt());
                // Debug.LogError(deltaReader.ReadInt());
            }
        }
    }
}