using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Server.Cheats;
using System;
using System.Collections;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Neutron/Neutron Rigidbody")]
    public class NeutronRigidbody : NeutronBehaviour
    {
        [Header("[Synchronize Settings]")]
        [SerializeField] private bool synchronizeVelocity = true;
        [SerializeField] private bool synchronizePosition = true;
        [SerializeField] private bool synchronizeRotation = true;
        [SerializeField] private bool synchronizeAngularVelocity = true;

        [Header("[Smooth Settings]")]
        [SerializeField] [Range(0, 1f)] private float synchronizeInterval = 0.1f;
        [SerializeField] [Range(0, 30f)] private float Smooth = 2f;
        [SerializeField] private bool smoothOnServer = true;

        [Header("[Lag Compensation Settings]")]
        [SerializeField] private bool lagCompensation = true;
        [SerializeField] private float lagMultiplier = 3f;

        [Header("[Cheater Settings]")]
        [SerializeField] private bool antiTeleport = true;
        [SerializeField] private float teleportIfDistanceGreaterThan = 12f;
        [SerializeField] private float isCheaterIfDistanceGreaterThan = 15f;
        [SerializeField] private bool antiSpeedHack = true;

        [Header("[General Settings]")]
        [SerializeField] SmoothMode smoothMode = SmoothMode.MoveTowards;
        [SerializeField] private SendTo sendTo = SendTo.Others;
        [SerializeField] private Broadcast broadcast = Broadcast.Room;
        [SerializeField] private Protocol protocol = Protocol.Udp;

        [Header("[Infor]")]
        [SerializeField] [ReadOnly] private int currentPacketsPerSecond;
        [SerializeField] [ReadOnly] private int maxPacketsPerSecond;
        private Vector3 position;
        private Quaternion rotation;
        private Rigidbody neutronRigidbody;
        private bool onFirstPacket = false;

        private new void Awake()
        {
            base.Awake();
            neutronRigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            if (IsClient && (synchronizeVelocity || synchronizeRotation) && HasAuthority)
                StartCoroutine(Synchronize());
            else if (IsServer) maxPacketsPerSecond = GetMaxPacketsPerSecond(synchronizeInterval);
        }

        private void Start()
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (IsServer && !HasAuthority && !IsClient && antiSpeedHack)
                StartCoroutine(PacketSpeed());
#endif
        }

        private IEnumerator Synchronize()
        {
            while (true)
            {
                using (NeutronWriter options = new NeutronWriter())
                {
                    if (synchronizePosition) options.Write(neutronRigidbody.position);
                    if (synchronizeVelocity) options.Write(neutronRigidbody.velocity);
                    if (synchronizeRotation) options.Write(neutronRigidbody.rotation);
                    if (synchronizeAngularVelocity) options.Write(neutronRigidbody.angularVelocity);
                    if (neutronRigidbody.velocity != Vector3.zero && synchronizeVelocity || neutronRigidbody.angularVelocity != Vector3.zero && synchronizeAngularVelocity)
                        Dynamic(10012, options, sendTo, false, broadcast, protocol);
                }
                yield return new WaitForSeconds(synchronizeInterval);
            }
        }

        [Dynamic(10012)]
        private void RPC(NeutronReader options, Player sender, NeutronMessageInfo infor)
        {
            onFirstPacket = true;
            using (options)
            {
#if UNITY_SERVER || UNITY_EDITOR
                if (IsServer)
                    ++currentPacketsPerSecond;
#endif
                if (synchronizePosition) position = options.ReadVector3();
                if (synchronizeVelocity) neutronRigidbody.velocity = options.ReadVector3();
                if (synchronizeRotation) rotation = options.ReadQuaternion();
                if (synchronizeAngularVelocity) neutronRigidbody.angularVelocity = options.ReadVector3();

                if (IsServer && lagCompensation)
                {
                    float lag = (float)Math.Abs(Neutron.Server.CurrentTime - infor.SentClientTime) + synchronizeInterval;
                    position += neutronRigidbody.velocity * (lag / lagMultiplier);
                }
                if (synchronizePosition) TeleportByDistance();
            }
        }

        private void TeleportByDistance()
        {
            Vector3 lagDistance = position - transform.position;
            if (lagDistance.magnitude > teleportIfDistanceGreaterThan)
            {
#if UNITY_SERVER || UNITY_EDITOR
                if (IsServer && antiTeleport)
                    CheatsUtils.Teleport(lagDistance, (teleportIfDistanceGreaterThan + isCheaterIfDistanceGreaterThan), NeutronView.owner);
#endif
                transform.position = position;
            }
        }

        private IEnumerator PacketSpeed()
        {
            while (IsServer)
            {
                CheatsUtils.SpeedHack(currentPacketsPerSecond, maxPacketsPerSecond, NeutronView.owner);
                currentPacketsPerSecond = 0;
                yield return new WaitForSeconds(1f);
            }
        }

        private void SmoothMovement()
        {
            if (smoothMode == SmoothMode.MoveTowards)
            {
                if (synchronizePosition) neutronRigidbody.position = Vector3.MoveTowards(neutronRigidbody.position, position, Smooth * Time.fixedDeltaTime);
                if (synchronizeRotation) neutronRigidbody.rotation = Quaternion.RotateTowards(neutronRigidbody.rotation, rotation, Smooth * Time.fixedDeltaTime);
            }
            else if (smoothMode == SmoothMode.Lerp)
            {
                if (synchronizePosition) neutronRigidbody.position = Vector3.Lerp(neutronRigidbody.position, position, Smooth * Time.fixedDeltaTime);
                if (synchronizeRotation) neutronRigidbody.rotation = Quaternion.Slerp(neutronRigidbody.rotation, rotation, Smooth * Time.fixedDeltaTime);
            }
        }

        private new void FixedUpdate()
        {
            base.FixedUpdate();
            if (!HasAuthority && !onFirstPacket) return;
            if (IsClient)
            {
                if (!HasAuthority)
                {
                    SmoothMovement();
                }
            }
            else if (IsServer)
            {
                if (!HasAuthority)
                {
                    if (smoothOnServer)
                        SmoothMovement();
                    else
                    {
                        if (synchronizePosition) neutronRigidbody.position = position;
                        if (synchronizeRotation) neutronRigidbody.rotation = rotation;
                    }
                }
            }
        }
    }
}