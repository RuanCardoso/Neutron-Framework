using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
#endif

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Internal
{
    ///* Esta classe é a base do objeto de rede(NeutronView).
    public class ViewBehaviour : MonoBehaviour
    {
        #region Fields -> Public
        /// <summary>
        ///* Este ID é usado para identificar a instância que irá invocar os iRPC'S.
        /// </summary>
        [ReadOnly] public int Id;
        /// <summary>
        ///* Define o ambiente que o objeto deve ser criado, Client, Server ou ambos.
        /// </summary>
        public Side Side = Side.Both;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o jogador que é dono do objeto.
        /// </summary>
        public NeutronPlayer Player { get; set; }
        /// <summary>
        ///* A instância de Neutron a qual este objeto pertence.
        /// </summary>
        public Neutron This { get; set; }
        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do servidor.
        /// </summary>
        public bool IsServer { get; set; }
        /// <summary>
        ///* Retorna se o objeto é um objeto de cena, falso, se for um jogador.
        /// </summary>
        public bool IsSceneObject => RegisterType == RegisterMode.Scene;
        /// <summary>
        ///* O tipo de registro usado para o objeto.
        /// </summary>
        public RegisterMode RegisterType { get; set; }
        /// <summary>
        ///* Define o matchmaking em que esse objeto existe.
        /// </summary>
        public INeutronMatchmaking Matchmaking { get; set; }
        #endregion

        #region Collections
        ///* Aqui será armazenado todos os metódos marcado com o atributo iRPC.
        [NonSerialized] public Dictionary<(byte, byte), RPCInvoker> iRPCs = new Dictionary<(byte, byte), RPCInvoker>();
        [NonSerialized] public Dictionary<int, NeutronBehaviour> NeutronBehaviours = new Dictionary<int, NeutronBehaviour>();
        #endregion

        #region Default Properties
        /// <summary>
        ///* Esta propriedade é usada para sincronizar a posição atual do objeto em todos os clientes que ingressarem posteriormente.
        /// </summary>
        public Vector3 LastPosition { get; set; }
        /// <summary>
        ///* Esta propriedade é usada para sincronizar a rotação atual do objeto em todos os clientes que ingressarem posteriormente.
        /// </summary>
        public Quaternion LastRotation { get; set; }
        /// <summary>
        ///* Obtém o transform anexado ao objeto.
        /// </summary>
        public Transform Transform { get; set; }
        #endregion

        #region Mono Behaviour
        private void Awake() => GetRpcs();

        private void Start()
        {
            Transform = transform;
            NeutronModule.OnUpdate += OnNeutronUpdate;
        }

        private void OnDestroy()
        {
            NeutronModule.OnUpdate -= OnNeutronUpdate;
        }

        //* Impede que objetos filhos tenham objeto de rede, caso o pai já tenha um.
        private void OnEnable()
        {
#if UNITY_EDITOR
            if (gameObject.activeInHierarchy)
            {
                foreach (Transform tr in transform)
                {
                    if (tr.TryGetComponent<NeutronView>(out NeutronView _))
                    {
                        if (!LogHelper.Error("Child objects cannot have \"NeutronView\", because their parent already has one."))
                            Destroy(gameObject);
                    }
                    else
                        continue;
                }
            }
#endif
        }

        private void OnNeutronUpdate()
        {
            LastPosition = Transform.position;
            LastRotation = Transform.rotation;
        }

        private void Reset()
        {
#if UNITY_EDITOR
            Id = 0;
            OnValidate();
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (gameObject.activeInHierarchy)
                {
                    if (Id == 0)
                        Id = UnityEngine.Random.Range(1, short.MaxValue);
                    else
                    {
                        int duplicated = FindObjectsOfType<NeutronView>().Count(x => x.Id == Id);
                        if (duplicated > 1)
                            Reset();
                    }
                }
                else
                    Id = 0;
            }
#endif
        }
        #endregion

        #region Virtual Methods
        /// <summary>
        ///* Este método é chamado quando o objeto é instanciado e está pronto para uso, não tenho certeza, depois vejo.
        /// </summary>
        public virtual void OnNeutronStart() { }
        /// <summary>
        ///* Este método é chamado quando o objeto é registrado na rede, não tenho certeza, depois vejo.
        /// </summary>
        public virtual void OnNeutronAwake() { }
        /// <summary>
        ///* Registra seu objeto em rede.
        /// </summary>
        public virtual bool OnNeutronRegister(NeutronPlayer player, bool isServer, RegisterMode registerType, Neutron instance, int dynamicId = 0) => true;
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é instanciado.
        private void GetRpcs()
        {
            var childs = GetComponentsInChildren<NeutronBehaviour>(); //* pega todas as instâncias que herdam de NeutronBehaviour
            if (childs.Length > 0)
            {
                //* Percorre as instâncias e pega os metódos e armazena no dicionário iRPC'S.
                for (int c = 0; c < childs.Length; c++)
                {
                    NeutronBehaviour child = childs[c];

                    #region Add Instances
                    if (!NeutronBehaviours.ContainsKey(child.ID))
                        NeutronBehaviours.Add(child.ID, child); //* Adiciona a instância no dict, para manter rápido acesso a qualquer instância.
                    else
                        LogHelper.Error($"Duplicate \"NeutronBehaviour\" ID not allowed in \"{child.GetType().Name}\". {child.ID}");
                    #endregion

                    if (child != null)
                    {
                        (iRPC[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<iRPC>(child);
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            (iRPC[], MethodInfo) methods = multiplesMethods[i];
                            for (int ii = 0; ii < methods.Item1.Count(); ii++)
                            {
                                iRPC method = methods.Item1[ii];
                                (byte, byte) key = (method.ID, child.ID);
                                if (!iRPCs.ContainsKey(key)) //* Verifica se não existe um metódo duplicado, ou seja, um iRPC com o mesmo ID.
                                    iRPCs.Add(key, new RPCInvoker(child, methods.Item2, method)); //* Adiciona o método no Dict, e monta sua estrutura RPC.
                                else
                                    LogHelper.Error($"Duplicate ID not allowed in \"{child.GetType().Name}\".");
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}