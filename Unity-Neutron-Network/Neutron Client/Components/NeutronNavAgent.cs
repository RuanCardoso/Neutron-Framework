using NeutronNetwork.Extesions;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.AI;

namespace NeutronNetwork.Components
{
    [RequireComponent(typeof(NavMeshAgent))]
    [AddComponentMenu("Neutron/NeutronNavAgent")]
    public class NeutronNavAgent : NeutronBehaviour
    {
        [SerializeField] private bool avoidOnlyEditorMode = false;

        [SerializeField] private Protocol protocolType = Protocol.Tcp;

        [SerializeField] private SendTo sendTo;

        [SerializeField] private Broadcast broadcast;

        private NavMeshAgent agent;

        private void OnValidate()
        {
            if (protocolType != Protocol.Tcp && protocolType != Protocol.Udp) protocolType = Protocol.Tcp;
        }

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            //----------------------------------------------------------------------//
            if (Application.isEditor)
                if (!avoidOnlyEditorMode) agent.radius = 0.01f;
            //----------------------------------------------------------------------//
            agent.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }

        void Update()
        {
            if (IsMine)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 500))
                    {
                        using (NeutronWriter streamParams = new NeutronWriter())
                        {
                            streamParams.Write(hit.point);
                            NeutronView._.RPC(252, 0, streamParams, sendTo, false, broadcast, (Protocol)(int)protocolType);
                        }
                    }
                }
            }
        }

        [RPC(252)]
        void Navsync(NeutronReader streamReader, bool isServer)
        {
            using (streamReader)
            {
                if (isServer)
                {
                    Vector3 point = streamReader.ReadVector3();
                    //-----------------------------------------------------------
                    agent.SetDestination(point);
                    //-----------------------------------------------------------
                    using (NeutronWriter streamParams = new NeutronWriter())
                    {
                        streamParams.Write(point);
                        streamParams.Write(NeutronView.transform.position);
                        NeutronView.APC(252, streamParams, SendTo.All, Broadcast.Channel);
                    }
                }
            }
        }

        [APC(252)]
        void Navsync(NeutronReader streamReader)
        {
            using (streamReader)
            {
                Vector3 point = streamReader.ReadVector3();
                Vector3 localPosition = streamReader.ReadVector3();
                //------------------------------------------------------
                agent.SetDestination(point);
            }
        }
    }
}