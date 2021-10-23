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
    /// <summary>
    ///* Fornece suporte a operações gRPC.
    /// </summary>
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
        #endregion

        #region Properties
        private Neutron Server => Neutron.Server.Instance;
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
        public NeutronStream.IWriter Begin_gRPC(NeutronStream parameters) => Server.Begin_gRPC(parameters);

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
            Server.End_gRPC(id, parameters, protocol, player);
        }
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é inicializado.
        private void MakeAttributes()
        {
            var instances = FindObjectsOfType<GlobalBehaviour>();
            GlobalBehaviour localInstance = this;
            Type localType = localInstance.GetType();
            (gRPCAttribute[], MethodInfo)[] ___ = ReflectionHelper.GetMultipleAttributesWithMethod<gRPCAttribute>(localInstance);
            if ((localType.BaseType == typeof(PlayerGlobalController) || localType.BaseType == typeof(NeutronBehaviour)) && ___.Length > 0)
                throw new NeutronException($"The class \"{localType.Name}\", they cannot declare \"[gRPC]\" methods, but they can invoke it, to solve this problem you must declare the method in a globally unique script, for example, \"ClientController\" or \"ServerController\"");
            if (instances[instances.Length - 1] == localInstance)
            {
                for (int gI = 0; gI < instances.Length; gI++)
                {
                    GlobalBehaviour instance = instances[gI];
                    if (instance != null && instance.enabled)
                    {
                        Type type = instance.GetType();
                        (gRPCAttribute[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<gRPCAttribute>(instance);
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            (gRPCAttribute[], MethodInfo) methods = multiplesMethods[i];
                            for (int ii = 0; ii < methods.Item1.Count(); ii++)
                            {
                                gRPCAttribute method = methods.Item1[ii];
                                if (!gRPCs.ContainsKey(method.ID)) //* Verifica se não existe um metódo duplicado, ou seja, um gRPC com mesmo ID.
                                    gRPCs.Add(method.ID, new RPCInvoker(instance, methods.Item2, method));
                                else
                                    throw new NeutronException($"gRPC: Duplicate Id not allowed in \"{type.Name}\" Id -> [{method.ID}]");
                            }
                        }
                    }
                    else
                        continue;
                }
            }
            else { /*continue;*/ }
        }
        #endregion
    }
}