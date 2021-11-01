using NeutronNetwork.Constants;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Rigidbody")]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NeutronTransform))]
    public class NeutronRigidbody : NeutronBehaviour
    {
        private const byte RpcId = 1;

        [Header("Smooth")]
#pragma warning disable IDE0044
        [SerializeField] [Range(NeutronConstantsSettings.MIN_SEND_RATE, NeutronConstantsSettings.MAX_SEND_RATE)] private int _packetsPerSecond = 50;
#pragma warning restore IDE0044

        private Rigidbody _rb;
        private double _lastSyncedTime;
        private Vector3 _velocity, _angularVelocity, _position;
        private bool _sync;
        protected override void Reset()
        {
            base.Reset();
            {
#if UNITY_EDITOR
                var option = _iRpcOptions.Find(x => x.RpcId == RpcId);
                option.TargetTo = Packets.TargetTo.Others;
                option.Protocol = Packets.Protocol.Udp;
                if (NeutronAuthority == null)
                    HandledBy(transform.GetComponent<NeutronTransform>());
#endif
            }
        }

        protected override void Awake()
        {
            base.Awake();
            {
                _rb = GetComponent<Rigidbody>();
            }
        }

        protected override void OnNeutronFixedUpdate()
        {
            base.OnNeutronFixedUpdate();
            {
                if (HasAuthority)
                {
                    if (LocalTime >= (_lastSyncedTime + (1d / _packetsPerSecond)))
                    {
                        using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                        {
                            var writer = Begin_iRPC(1, stream, out var option);
                            writer.Write(_rb.position);
                            writer.Write(_rb.velocity);
                            writer.Write(_rb.angularVelocity);
                            writer.Write(LocalTime);
                            writer.Write();
                            End_iRPC(1, stream);
                        }
                        _lastSyncedTime = LocalTime;
                    }
                }
                else
                {
                    if (_sync)
                    {
                        _rb.position = _position;
                        _rb.velocity = _velocity;
                        _rb.angularVelocity = _angularVelocity;
                    }
                }
            }
        }

        private double _lastTime;
        [iRPC(RpcId)]
        public void SyncRigidbody(NeutronStream.IReader reader, NeutronPlayer player)
        {
            if (DoNotPerformTheOperationOnTheServer)
            {
                if (!_sync)
                    _sync = true;

                var position = reader.ReadVector3();
                var velocity = reader.ReadVector3();
                var angularVelocity = reader.ReadVector3();
                var localTime = reader.ReadDouble();

                if (localTime > _lastTime)
                {
                    _position = position;
                    _velocity = velocity;
                    _angularVelocity = angularVelocity;
                }

                _lastTime = localTime;
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            {
                _offlineMode = false;
            }
        }
    }
}