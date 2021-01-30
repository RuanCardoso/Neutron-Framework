using NeutronNetwork.Internal.Extesions;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Neutron/Neutron Animator")]
    public class NeutronAnimator : NeutronBehaviour
    {
        [SerializeField] private ProtocolType protocolType = ProtocolType.Tcp;

        [Range(0, 1)]
        [SerializeField] private float syncTime = 0.3f;

        [SerializeField] private Animator GetAnimator;

        [SerializeField] private SendTo sendTo;

        [SerializeField] private Broadcast broadcast;

        private void OnValidate()
        {
            if (protocolType != ProtocolType.Tcp && protocolType != ProtocolType.Udp) protocolType = ProtocolType.Tcp;
        }

        bool GetParameters(out object[] mParams)
        {
            object[] parametersToSend = new object[GetAnimator.parameterCount];

            for (int i = 0; i < parametersToSend.Length; i++)
            {
                AnimatorControllerParameter parameter = GetAnimator.GetParameter(i);
                if (parameter.type == AnimatorControllerParameterType.Bool)
                {
                    parametersToSend[i] = GetAnimator.GetBool(parameter.name);
                }
                else if (parameter.type == AnimatorControllerParameterType.Float)
                {
                    parametersToSend[i] = GetAnimator.GetFloat(parameter.name);
                }
                else if (parameter.type == AnimatorControllerParameterType.Int)
                {
                    parametersToSend[i] = GetAnimator.GetInteger(parameter.name);
                }
            }
            mParams = parametersToSend;
            //===========================//
            return true;
        }

        // Update is called once per frame
        void Update()
        {
            if (IsMine)
            {
                if (GetParameters(out object[] parameters))
                {
                    using (NeutronWriter streamParams = new NeutronWriter())
                    {
                        streamParams.Write(parameters.Serialize());
                        //-----------------------------------------------------------------------------------------------------------------------------
                        if (ClientView._.ClientView == null)
                        {
                            Utils.LoggerError("ClientView is null");
                            return;
                        }
                        ClientView._.RPC(this, 254, syncTime, streamParams, sendTo, false, broadcast, (ProtocolType)(int)protocolType);
                    }
                }
            }
        }

        [RPC(254)]
        void Sync(NeutronReader streamReader, bool isServer)
        {
            using (streamReader)
            {
                object[] parameters = streamReader.ReadBytes(8192).DeserializeObject<object[]>();
                for (int i = 0; i < parameters.Length; i++)
                {
                    AnimatorControllerParameter parameter = GetAnimator.GetParameter(i);
                    if (parameter.type == AnimatorControllerParameterType.Bool)
                    {
                        GetAnimator.SetBool(parameter.name, (bool)parameters[i]);
                    }
                    else if (parameter.type == AnimatorControllerParameterType.Float)
                    {
                        GetAnimator.SetFloat(parameter.name, (float)parameters[i]);
                    }
                    else if (parameter.type == AnimatorControllerParameterType.Int)
                    {
                        GetAnimator.SetInteger(parameter.name, (int)parameters[i]);
                    }
                }
            }
        }
    }
}