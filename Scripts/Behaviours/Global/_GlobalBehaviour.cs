using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Packets;
using System;
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
#pragma warning disable IDE1006
        public static Dictionary<byte, RPCInvoker> gRPCs {
#pragma warning restore IDE1006
            get;
        } = new Dictionary<byte, RPCInvoker>();

        //* Usado apenas para checar duplicatas...
        private static readonly Dictionary<(int, int), int> _ids = new Dictionary<(int, int), int>();
        #endregion

        #region Properties
        private Neutron Instance => Neutron.Server.Instance;
        #endregion

        #region Mono Behaviour
        protected virtual void Awake() => MakeAttributes();
        #endregion

        #region Neutron
        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.<br/>
        ///* Prepara uma chamada gRPC na rede.<br/>
        ///* (Client Side).
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        public NeutronStream.IWriter Begin_gRPC(NeutronStream parameters, Neutron neutron) => neutron.Begin_gRPC(parameters);

        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.<br/>
        ///* Prepara uma chamada gRPC na rede.<br/>
        ///* (Server Side).
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        public NeutronStream.IWriter Begin_gRPC(NeutronStream parameters) => Instance.Begin_gRPC(parameters);

        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.<br/>
        ///* (Client Side) Client->Server->Clients.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
        /// <param name="neutron">* A instância de Neutron que realizará a comunicação.</param>
        public void End_gRPC(byte id, NeutronStream parameters, Protocol protocol, Neutron neutron)
        {
            neutron.End_gRPC(id, parameters, protocol);
        }

        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.<br/>
        ///* (Server Side) Server->Clients.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
        public void End_gRPC(byte id, NeutronStream parameters, Protocol protocol, NeutronPlayer player)
        {
            Instance.End_gRPC(id, parameters, protocol, player);
        }
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é inicializado.
        private void MakeAttributes()
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
                            gRPCs.Add(method.ID, new RPCInvoker(instance, methods.Item2, method));
                        //* Encontra duplicatas por instância.
                        (int, int) idsKey = (instance.GetInstanceID(), method.ID);
                        if (!_ids.ContainsKey(idsKey))
                            _ids.Add(idsKey, 0);
                        else
                            throw new Exception($"gRPC: Duplicate Id not allowed in \"{instance.GetType().Name}\" Id -> [{method.ID}]");
                    }
                }
            }
        }
        #endregion
    }
}