using NeutronNetwork.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_CONNECTION)]
    public class NeutronNonDynamicBehaviour : MonoBehaviour
    {
        #region Collections
        //* Aqui será armazenado todos os sRPC's.
        public static Dictionary<int, RemoteProceduralCall> sRPCs = new Dictionary<int, RemoteProceduralCall>();
        #endregion

        #region MonoBehaviour
        private void OnEnable()
        {
            GetAttributes();
        }
        #endregion

        #region Neutron
        /// <summary>
        ///* sRPC(Static Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* Envie o sRPC para um jogador específico, suporta o roteamento dos dados.<br/>
        ///* (Server Side) Server->Client.
        /// </summary>
        /// <param name="nSRPCId">* ID do metódo que será invocado.</param>
        /// <param name="nParameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nSender">* O jogador de destino da mensagem.</param>
        public void sRPC(int nSRPCId, NeutronWriter nParameters, Player nSender)
        {
            Neutron.Server.sRPC(nSender, nSRPCId, nParameters);
        }

        /// <summary>
        ///* sRPC(Static Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* (Client Side) Client->Server.
        /// </summary>
        /// <param name="nSRPCId">* ID do metódo que será invocado.</param>
        /// <param name="nParameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nProtocol">* O protocolo que será usado para enviar os dados.</param>
        /// <param name="nNeutron">* A instância de Neutron que realizará a comunicação.</param>
        protected void sRPC(int nSRPCId, NeutronWriter nParameters, Protocol nProtocol, Neutron nNeutron)
        {
            nNeutron.sRPC(nNeutron.MyPlayer.ID, nSRPCId, nParameters, nProtocol);
        }

        /// <summary>
        ///* sRPC(Static Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* Envie o sRPC para um jogador específico, suporta o roteamento dos dados.<br/>
        ///* (Client Side) Client->Server.
        /// </summary>
        /// <param name="nPlayer">* O jogador de destino da mensagem.</param>
        /// <param name="nSRPCId">* ID do metódo que será invocado.</param>
        /// <param name="nParameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nProtocol">* O protocolo que será usado para enviar os dados.</param>
        /// <param name="nNeutron">* A instância de Neutron que realizará a comunicação.</param>
        protected void sRPC(Player nPlayer, int nSRPCId, NeutronWriter nParameters, Protocol nProtocol, Neutron nNeutron)
        {
            nNeutron.sRPC(nPlayer.ID, nSRPCId, nParameters, nProtocol);
        }
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é inicializado.
        private void GetAttributes()
        {
            NeutronNonDynamicBehaviour mInstance = this; //* pega a instância atual que contém os metódos sRPC, herança.
            if (mInstance != null)
            {
                var mType = mInstance.GetType();
                MethodInfo[] mInfos = mType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                //* Percorre as instâncias e pega os metódos e armazena no dicionário sRPC'S.
                for (int y = 0; y < mInfos.Length; y++) //* Percorre a parada.
                {
                    sRPC[] Attrs = mInfos[y].GetCustomAttributes<sRPC>().ToArray(); //* pega todos os attributos sRPC do metódo.
                    if (Attrs != null)
                    {
                        foreach (sRPC Attr in Attrs)
                        {
                            if (!sRPCs.ContainsKey(Attr.ID)) //* Verifica se não existe um metódo duplicado, ou seja, um sRPC com mesmo ID.
                                sRPCs.Add(Attr.ID, new RemoteProceduralCall(mInstance, mInfos[y], Attr)); //* Monta sua estrutura e armazena no Dict.
                            else
                                NeutronLogger.Print($"Duplicate ID not allowed in \"{mType.Name}\".");
                        }
                    }
                    else continue;
                }
            }
        }
        #endregion
    }
}