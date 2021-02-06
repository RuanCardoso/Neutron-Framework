using UnityEngine;
using UnityEngine.AI;

namespace NeutronNetwork.Components
{
    public class Resync : NeutronBehaviour
    {
        NavMeshAgent agent;
        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            //------------------------------------
            if (agent == null) Destroy(this);
        }

        private void Update()
        {
            UpdateResync();
        }

        public void UpdateResync()
        {
            if (IsMine)
            {
                if (agent.velocity.magnitude == 0)
                {
                    using (NeutronWriter options = new NeutronWriter())
                    {
                        options.Write(transform.position);
                        options.Write(transform.rotation);
                        NeutronView._.RPC(1005, 0.5f, options, SendTo.Others, false, Broadcast.Channel, Protocol.Udp);
                    }
                }
            }
        }

        [RPC(1005)]
        void InternalResync(NeutronReader read, bool isServer)
        {
            if (!isServer)
            {
                Vector3 vector3 = read.ReadVector3();
                Quaternion quaternion = read.ReadQuaternion();
                transform.position = vector3;
                transform.rotation = quaternion;
            }
        }
    }
}