using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Server.Cheats;
using System;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [Flags]
    public enum WhenChanging : int
    {
        Position = 1,
        Rotation = 2,
        Velocity = 4,
        AngularVelocity = 8,
    }

    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Neutron/Neutron Rigidbody")]
    public class NeutronRigidbody : NeutronBehaviour
    {
        private delegate void OnChanged();
        private event OnChanged onChanged;

        [SerializeField] private WhenChanging whenChanging;
        [SerializeField] private SendTo sendTo = SendTo.Others;
        [SerializeField] private Broadcast broadcast;
        [SerializeField] private Protocol protocol = Protocol.Tcp;

        [SerializeField] [Range(0, 10)] private float syncTime = 0.1f;
        [SerializeField] [Range(0, 10)] private float smoothSync = 5f;
        [SerializeField] private bool isCached;

        private Vector3 velocity;
        private Vector3 angularVelocity;
        private Vector3 position;
        private Quaternion rotation;

        private Rigidbody _rigidbody;

        private Vector3 Velocity {
            get => velocity;
            set {
                if (whenChanging.HasFlag(WhenChanging.Velocity))
                {
                    if (velocity != value)
                        onChanged?.Invoke();
                }
                velocity = value;
            }
        }
        private Vector3 AngularVelocity {
            get => angularVelocity;
            set {
                if (whenChanging.HasFlag(WhenChanging.AngularVelocity))
                {
                    if (angularVelocity != value)
                        onChanged?.Invoke();
                }
                angularVelocity = value;
            }
        }
        private Vector3 Position {
            get => position;
            set {
                if (whenChanging.HasFlag(WhenChanging.Position))
                {
                    if (position != value)
                        onChanged?.Invoke();
                }
                position = value;
            }
        }
        private Quaternion Rotation {
            get => rotation;
            set {
                if (whenChanging.HasFlag(WhenChanging.Rotation))
                {
                    if (rotation != value)
                        onChanged?.Invoke();
                }
                rotation = value;
            }
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            if (IsMine)
                onChanged += NeutronRigidbody_OnChanged;
        }

        private void Update()
        {
            if (!IsMine)
            {
                if (Position != Vector3.zero)
                    transform.position = Vector3.Lerp(transform.position, Position, Time.deltaTime * smoothSync);
                if (Rotation != Quaternion.identity)
                    transform.rotation = Quaternion.Lerp(transform.rotation, Rotation, Time.deltaTime * smoothSync);
                return;
            }
            if (whenChanging == default) onChanged?.Invoke();
            Position = transform.position;
            Rotation = transform.rotation;
            Velocity = _rigidbody.velocity;
            AngularVelocity = _rigidbody.angularVelocity;
        }

        private void FixedUpdate()
        {
            if (IsMine) return;
            _rigidbody.velocity = Velocity;
            _rigidbody.angularVelocity = AngularVelocity;
        }

        private void NeutronRigidbody_OnChanged()
        {
            using (NeutronWriter writeParams = new NeutronWriter())
            {
                writeParams.Write(Position);
                writeParams.Write(Rotation);
                writeParams.Write(Velocity);
                writeParams.Write(AngularVelocity);
                NeutronView._.RPC(1002, syncTime, writeParams, sendTo, isCached, broadcast, protocol);
            }
        }

        [RPC(1002)]
#pragma warning disable IDE0051 // Remover membros privados não utilizados
        void SyncRigidbody(NeutronReader readParams)
#pragma warning restore IDE0051 // Remover membros privados não utilizados
        {
            using (readParams)
            {
                Position = readParams.ReadVector3();
                Rotation = readParams.ReadQuaternion();
                Velocity = readParams.ReadVector3();
                AngularVelocity = readParams.ReadVector3();
            }
        }
    }
}