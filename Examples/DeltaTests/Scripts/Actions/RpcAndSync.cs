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

        private double lastSend;
        protected override void OnNeutronUpdate()
        {
            base.OnNeutronUpdate();
            {
                if (HasAuthority)
                {
                    if (LocalTime > 0)
                    {
                        for (int i = 0; i < f; i++)
                        {
                            var writer = Begin_iRPC(1, _stream, out var _);
                            writer.Write(11210);
                            writer.Write(11210);
                            writer.Write(11210);
                            writer.Write(11210);
                            writer.Write();
                            End_iRPC(1, _stream);
                        }
                        lastSend = LocalTime;
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
            LogHelper.Error("??????");
        }
    }
}