using System.Collections;
using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Server.Cheats;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Transform")]
    public class NeutronTransform : NeutronBehaviour
    {
        [Header("[Synchronize Settings]")]
        [SerializeField] private bool synchronizePosition = true;
        [SerializeField] private bool synchronizeRotation = true;
        [SerializeField] private bool synchronizeScale;

        [Header("[Smooth Settings]")]
        [SerializeField] [Range(0, 1f)] private float synchronizeInterval = 0.1f;
        [SerializeField] [Range(0, 30f)] private float Smooth = 10f;
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
        [SerializeField] SmoothMode smoothMode;
        [SerializeField] private SendTo sendTo = SendTo.Others;
        [SerializeField] private Broadcast broadcast = Broadcast.Room;
        [SerializeField] private Protocol protocol = Protocol.Udp;

        [Header("[Infor]")]
        [SerializeField] [ReadOnly] private int currentPacketsPerSecond;
        [SerializeField] [ReadOnly] private int maxPacketsPerSecond;
        private Vector3 position, scale;
        private Quaternion rotation;
        private bool onFirstPacket = false;

        public override void OnNeutronStart()
        {
            if (IsClient && (synchronizePosition || synchronizeRotation || synchronizeScale) && IsMine)
                StartCoroutine(Synchronize());
            else if (IsServer) maxPacketsPerSecond = GetMaxPacketsPerSecond(synchronizeInterval);
        }

        private void Start()
        {
#if UNITY_SERVER || UNITY_EDITOR
            if (IsServer && !IsMine && !IsClient && antiSpeedHack)
                StartCoroutine(PacketSpeed());
#endif
        }

        private void ResetTransforms()
        {
            if (synchronizePosition) position = transform.position;
            if (synchronizeRotation) rotation = transform.rotation;
            if (synchronizeScale) scale = transform.localScale;
        }

        private IEnumerator Synchronize()
        {
            while (true)
            {
                using (NeutronWriter options = new NeutronWriter())
                {
                    if (synchronizePosition) options.Write(transform.position);
                    if (synchronizeRotation) options.Write(transform.rotation);
                    if (synchronizeScale) options.Write(transform.localScale);
                    if (transform.position != position && synchronizePosition || transform.rotation != rotation && synchronizeRotation || transform.localScale != scale && synchronizeScale)
                        NeutronView._.RPC(10013, options, sendTo, false, broadcast, protocol);
                    if (sendTo == SendTo.Others || sendTo == SendTo.Only)
                        ResetTransforms();
                }
                yield return new WaitForSeconds(synchronizeInterval);
            }
        }

        [RPC(10013)]
        private void RPC(NeutronReader options, Player sender, NeutronMessageInfo infor)
        {
            onFirstPacket = true;
            using (options)
            {
#if UNITY_SERVER || UNITY_EDITOR
                if (IsServer)
                    ++currentPacketsPerSecond;
#endif
                Vector3 oldPos = transform.position;
                if (synchronizePosition) position = options.ReadVector3();
                if (synchronizeRotation) rotation = options.ReadQuaternion();
                if (synchronizeScale) scale = options.ReadVector3();
                if (IsServer && lagCompensation)
                {
                    float lag = Mathf.Abs(Neutron.Server.CurrentTime - infor.SentClientTime) + synchronizeInterval;
                    position += (transform.position - oldPos) * (lag / lagMultiplier);
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
            if (smoothMode == SmoothMode.Lerp)
            {
                if (synchronizePosition) transform.position = Vector3.Lerp(transform.position, position, Smooth * Time.deltaTime);
                if (synchronizeRotation) transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Smooth * Time.deltaTime);
            }
            else if (smoothMode == SmoothMode.MoveTowards)
            {
                if (synchronizePosition) transform.position = Vector3.MoveTowards(transform.position, position, Smooth * Time.deltaTime);
                if (synchronizeRotation) transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, Smooth * Time.deltaTime);
            }
        }

        //* Update is called once per frame
        void Update()
        {
            if (!IsMine && !onFirstPacket) return;
            if (!IsMine && synchronizeScale) transform.localScale = scale;
            if (IsClient)
            {
                if (!IsMine)
                {
                    SmoothMovement();
                }
            }
            else if (IsServer)
            {
                if (!IsMine)
                {
                    if (smoothOnServer)
                        SmoothMovement();
                    else
                    {
                        if (synchronizePosition) transform.position = position;
                        if (synchronizeRotation) transform.rotation = rotation;
                    }
                }
            }
        }
    }
}