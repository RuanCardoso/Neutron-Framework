using NeutronNetwork.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONNECTION)]
    public class GlobalBehaviour : MonoBehaviour
    {
        #region Collections
        //* Aqui será armazenado todos os gRPC's, a chave é seu ID.
        public static Dictionary<byte, RPC> gRPCs = new Dictionary<byte, RPC>();
        #endregion

        #region Mono Behaviour
        public virtual void Awake() => GetRpcs();
        #endregion

        #region Neutron
        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* (Client Side) Client->Server->Clients.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
        /// <param name="neutron">* A instância de Neutron que realizará a comunicação.</param>
#pragma warning disable IDE1006
        protected void gRPC(byte id, NeutronWriter writer, Protocol protocol, Neutron neutron)
#pragma warning restore IDE1006
        {
            neutron.gRPC(id, writer, protocol);
        }

        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* (Server Side) Server->Clients.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
        /// <param name="neutron">* A instância de Neutron que realizará a comunicação.</param>
#pragma warning disable IDE1006
        protected void gRPC(byte id, NeutronWriter writer, Protocol protocol, NeutronPlayer player, Neutron neutron)
#pragma warning restore IDE1006
        {
            neutron.gRPC(id, writer, protocol, player);
        }
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é inicializado.
        private void GetRpcs()
        {
            GlobalBehaviour instance = this;
            if (instance != null)
            {
                (gRPC[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<gRPC>(instance);
                for (int i = 0; i < multiplesMethods.Length; i++)
                {
                    (gRPC[], MethodInfo) methods = multiplesMethods[i];
                    for (int ii = 0; ii < methods.Item1.Count(); ii++)
                    {
                        gRPC method = methods.Item1[ii];
                        if (!gRPCs.ContainsKey(method.ID)) //* Verifica se não existe um metódo duplicado, ou seja, um gRPC com mesmo ID.
                            gRPCs.Add(method.ID, new RPC(instance, methods.Item2, method)); //* Monta sua estrutura e armazena no Dict.
                        else
                            LogHelper.Error($"Duplicate ID not allowed in \"{instance.GetType().Name}\".");
                    }
                }
            }
        }
        #endregion
    }
}