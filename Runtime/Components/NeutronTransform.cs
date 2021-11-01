using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///* Thanks to: vis2k(Mirror).
/// </summary>
namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Transform")]
    public class NeutronTransform : NeutronBehaviour
    {
        private const byte RpcId = 1;

        private readonly SortedList<double, NetworkTransformSnapshot> _buffer = new SortedList<double, NetworkTransformSnapshot>();
        private readonly Func<NetworkTransformSnapshot, NetworkTransformSnapshot, double, NetworkTransformSnapshot> _interpolate = NetworkTransformSnapshot.Interpolate;
        private readonly object _bufferLock = new object();

        [Header("Compression")]
#pragma warning disable IDE0044
        [SerializeField] private bool _compressQuaternion = true;
        [SerializeField] [ShowIf("_compressQuaternion")] private float _floatMultiplicationPrecision = 10000f;
#pragma warning restore IDE0044

        [Header("Transform")]
#pragma warning disable IDE0044
        [SerializeField] private bool _syncPosition = true;
        [SerializeField] private bool _syncRotation = true;
        [SerializeField] private bool _syncScale;
#pragma warning restore IDE0044

        [Header("Properties")]
#pragma warning disable IDE0044
        [SerializeField] private float _teleportMaxDistance = 15f;
#pragma warning restore IDE0044

        [Header("Smooth")]
#pragma warning disable IDE0044
        [SerializeField] [Range(NeutronConstantsSettings.MIN_SEND_RATE, NeutronConstantsSettings.MAX_SEND_RATE)] private int _packetsPerSecond = 60;
#pragma warning restore IDE0044

        [Header("Buffer")]
        [SerializeField] [ReadOnly] private double _interpolationTime;
#pragma warning disable IDE0044
        [SerializeField] private int _bufferMaxCount = 6;
        [SerializeField] private float _bufferTime = 0f;
        [SerializeField] private int _catchupThreshold = 4;
        [SerializeField] [Range(0, 1)] private float _catchupMultiplier = 0.10f;
#pragma warning restore IDE0044

        private double _lastSyncedTime;
        protected override void Reset()
        {
            base.Reset();
            {
#if UNITY_EDITOR
                var option = _iRpcOptions.Find(x => x.RpcId == RpcId);
                option.TargetTo = Packets.TargetTo.Others;
                option.Protocol = Packets.Protocol.Udp;
#endif
            }
        }

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            { }
        }

        protected override void OnNeutronUpdate()
        {
            base.OnNeutronUpdate();
            {
                if (HasAuthority)
                {
                    if (LocalTime >= (_lastSyncedTime + (1d / _packetsPerSecond)))
                    {
                        using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                        {
                            var writer = Begin_iRPC(1, stream, out var option);
                            if (_syncPosition)
                                writer.Write(transform.localPosition);
                            if (_syncRotation)
                            {
                                if (_compressQuaternion)
                                    writer.WriteCompressed(transform.localRotation, _floatMultiplicationPrecision);
                                else
                                    writer.Write(transform.localRotation);
                            }
                            if (_syncScale)
                                writer.Write(transform.localScale);
                            writer.Write(LocalTime); //* timestamp
                            writer.Write();
                            End_iRPC(1, stream);
                        }
                        _lastSyncedTime = LocalTime;
                    }
                }
                else
                {
                    lock (_bufferLock)
                    {
                        //* Corrige a posi��o, se o buffer estiver grande, logo o processamento est� atrasado, limpa e continua....
                        if (_buffer.Count > _bufferMaxCount)
                            Clear();
                        if (SnapshotInterpolation.Compute(LocalTime, Time.deltaTime, ref _interpolationTime, _bufferTime, _buffer, _catchupThreshold, _catchupMultiplier, _interpolate, out NetworkTransformSnapshot computed))
                            Interpolate(computed);
                    }
                }
            }
        }

        private void Interpolate(NetworkTransformSnapshot interpolated)
        {
            if (_syncPosition)
                transform.localPosition = interpolated.position;
            if (_syncRotation)
                transform.localRotation = interpolated.rotation;
            if (_syncScale)
                transform.localScale = interpolated.scale;
        }

        private void OnTeleport(Vector3 position)
        {
            NeutronSchedule.ScheduleTask(() =>
            {
                if (!(Vector3.Distance(transform.position, position) > _teleportMaxDistance))
                    return;

                transform.localPosition = position;
                lock (_bufferLock)
                    Clear();
            });
        }

        private void Clear()
        {
            _buffer.Clear();
            _interpolationTime = 0;
        }

        [iRPC(RpcId)]
        public void SyncSnapshot(NeutronStream.IReader reader, NeutronPlayer player)
        {
            var position = _syncPosition ? reader.ReadVector3() : Vector3.zero;
            var rotation = _syncRotation ? _compressQuaternion ? reader.ReadCompressedQuaternion(_floatMultiplicationPrecision) : reader.ReadQuaternion() : Quaternion.identity;
            var scale = _syncScale ? reader.ReadVector3() : Vector3.zero;
            var timestamp = reader.ReadDouble();

            OnTeleport(position);

            NetworkTransformSnapshot snapshot = new NetworkTransformSnapshot(
                timestamp,
                LocalTime,
                position, rotation, scale
            );

            lock (_bufferLock)
                SnapshotInterpolation.InsertIfNewEnough(snapshot, _buffer);
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