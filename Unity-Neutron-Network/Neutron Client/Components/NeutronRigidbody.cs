using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Server.Cheats;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Neutron/Neutron Rigidbody")]
    public class NeutronRigidbody : NeutronBehaviour
    {
        public WhenChanging whenChanging;

        [SerializeField] private Protocol protocolType = Protocol.Tcp;

        [Range(0, 1)]
        [SerializeField] private float syncTime = 0.1f;
        [Range(0, 100)]
        [SerializeField] private float LerpTime = 8f;

        [SerializeField] private Rigidbody GetRigidbody;

        [SerializeField] private bool ClientOnly = false;

        [SerializeField] private SendTo sendTo;

        [SerializeField] private Broadcast broadcast;

        Vector3 oldVelocity, oldRotation, oldPosition;
        Vector3 newPosition, newVelocity, newAngularVelocity;
        Quaternion newRotation;

        float frequencyTime = 0;
        [SerializeField] float currentFrequency;

        private void OnValidate()
        {
            if (protocolType != Protocol.Tcp && protocolType != Protocol.Udp) protocolType = Protocol.Tcp;
        }

        void Update()
        {
            AntiSpeedhack();
            if (IsMine)
            {
                switch (whenChanging)
                {
                    case (WhenChanging)
                    default:
                        RPC();
                        break;
                    case WhenChanging.Position:
                        if (transform.position != oldPosition)
                        {
                            RPC();
                            oldPosition = transform.position;
                        }
                        break;
                    case WhenChanging.Rotation:
                        if (transform.eulerAngles != oldRotation)
                        {
                            RPC();
                            oldRotation = transform.eulerAngles;
                        }
                        break;
                    case WhenChanging.Velocity:
                        if (GetRigidbody.velocity != oldVelocity)
                        {
                            RPC();
                            oldVelocity = GetRigidbody.velocity;
                        }
                        break;
                    case (WhenChanging.Position | WhenChanging.Rotation):
                        if (transform.position != oldPosition || transform.eulerAngles != oldRotation)
                        {
                            RPC();
                            oldPosition = transform.position;
                            oldRotation = transform.eulerAngles;
                        }
                        break;
                    case (WhenChanging.Velocity | WhenChanging.Position):
                        if (transform.position != oldPosition || GetRigidbody.velocity != oldVelocity)
                        {
                            RPC();
                            oldPosition = transform.position;
                            oldVelocity = GetRigidbody.velocity;
                        }
                        break;
                    case (WhenChanging.Velocity | WhenChanging.Rotation):
                        if (transform.eulerAngles != oldRotation || GetRigidbody.velocity != oldVelocity)
                        {
                            RPC();
                            oldRotation = transform.eulerAngles;
                            oldVelocity = GetRigidbody.velocity;
                        }
                        break;
                    case (WhenChanging.Velocity | WhenChanging.Rotation | WhenChanging.Position):
                        AnyProperty();
                        break;
                    default:
                        AnyProperty();
                        break;
                }
            }
            else
            {
                if (newPosition != Vector3.zero && transform.position != newPosition) transform.position = Vector3.Lerp(transform.position, newPosition, LerpTime * Time.deltaTime);
                if (newPosition != Vector3.zero && transform.position != newPosition) transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, LerpTime * Time.deltaTime);
            }
        }

        void AntiSpeedhack()
        {
            if (!IsServer) return;
            //----------------------------------------------------------------------------------------------------------------------------------------------------------------------
            frequencyTime += Time.deltaTime;
            if (frequencyTime >= 1f)
            {
                CheatsUtils.AntiSpeedHack(currentFrequency, NeutronSConst.SPEEDHACK_TOLERANCE, NeutronView.owner);
                //--------------------------------------------------------------------------------------------------------------------------------------------------
                currentFrequency = 0;
                frequencyTime = 0;
            }
        }

        void AnyProperty()
        {
            if (transform.eulerAngles != oldRotation || GetRigidbody.velocity != oldVelocity || transform.position != oldPosition)
            {
                RPC();
                oldRotation = transform.eulerAngles;
                oldVelocity = GetRigidbody.velocity;
                oldPosition = transform.position;
            }
        }

        void RPC()
        {
            using (NeutronWriter streamParams = new NeutronWriter())
            {
                streamParams.Write(transform.position);
                streamParams.Write(transform.rotation);
                streamParams.Write(GetRigidbody.velocity);
                streamParams.Write(GetRigidbody.angularVelocity);
                NeutronView._.RPC(255, syncTime, streamParams, sendTo, false, broadcast, (Protocol)(int)protocolType);
            }
        }

        [RPC(255)]
        void Sync(NeutronReader streamReader, bool isServer)
        {
            using (streamReader)
            {

                if (ClientOnly && isServer) return;

                newPosition = streamReader.ReadVector3();
                newRotation = streamReader.ReadQuaternion();
                newVelocity = streamReader.ReadVector3();
                newAngularVelocity = streamReader.ReadVector3();

                if (!ClientOnly && isServer)
                {
                    currentFrequency++;
                    //------------------------------------------------------------------------------------------------------------------------------------------------------------------
                    CheatsUtils.AntiTeleport(transform.position, newPosition, NeutronSConst.TELEPORT_DISTANCE_TOLERANCE, NeutronView.owner);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsMine)
            {
                GetRigidbody.velocity = newVelocity;
                GetRigidbody.angularVelocity = newAngularVelocity;
            }
        }
    }
}