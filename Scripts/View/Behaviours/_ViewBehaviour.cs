using NeutronNetwork.Internal.Interfaces;
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
        [ReadOnly] public int ID;
        /// <summary>
        ///* Define o ambiente que o objeto deve ser criado, Client, Server ou ambos.
        /// </summary>
        public Side Side = Side.Both;
        /// <summary>
        ///* O tipo de registro usado para o objeto.
        /// </summary>
        [ReadOnly] public RegisterType RegisterType;
        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do servidor.
        /// </summary>
        [ReadOnly] public bool IsServer;
        #endregion

        #region Fields
        [SerializeField] private NeutronPlayer _player;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o jogador que é dono do objeto.
        /// </summary>
        public NeutronPlayer Player { get => _player; set => _player = value; }
        /// <summary>
        ///* A instância de Neutron a qual este objeto pertence.
        /// </summary>
        public Neutron This { get; set; }
        /// <summary>
        ///* Retorna se o objeto é um objeto de cena, falso, se for um jogador.
        /// </summary>
        public bool IsSceneObject => SceneHelper.IsSceneObject(ID);
        /// <summary>
        ///* Define o matchmaking em que esse objeto existe.
        /// </summary>
        public INeutronMatchmaking Matchmaking { get; set; }
        #endregion

        #region Collection
        ///* Aqui será armazenado todos os metódos marcado com o atributo iRPC.
        [NonSerialized] public Dictionary<(byte, byte), RPC> iRPCs = new Dictionary<(byte, byte), RPC>();
        [NonSerialized] public Dictionary<int, NeutronBehaviour> Childs = new Dictionary<int, NeutronBehaviour>();
        #endregion

        #region Default Properties
        /// <summary>
        ///* Esta propriedade é usada para sincronizar a posição atual do objeto em todos os clientes que ingressarem posteriormente.
        /// </summary>
        public Vector3 LastPosition { get; set; }
        /// <summary>
        ///* Esta propriedade é usada para sincronizar a rotação atual do objeto em todos os clientes que ingressarem posteriormente.
        /// </summary>
        public Vector3 LastRotation { get; set; }
        #endregion

        #region Mono Behaviour
        public void Awake() => GetIRPCS();

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
                    else continue;
                }
            }
#endif
        }

        public void Update()
        {
            LastPosition = transform.position;
            LastRotation = transform.eulerAngles;
        }

        private void Reset()
        {
#if UNITY_EDITOR
            ID = 0;
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
                    if (ID == 0)
                        ID = Math.Abs(GetInstanceID());
                    else
                    {
                        int count = FindObjectsOfType<NeutronView>().Count(x => x.ID == ID);
                        if (count > 1)
                            Reset();
                    }
                }
                else
                    ID = 0;
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
        public virtual bool OnNeutronRegister(NeutronPlayer player, bool isServer, RegisterType registerType, Neutron instance, int dynamicId = 0) => true;
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é instanciado.
        private void GetIRPCS()
        {
            var childs = GetComponentsInChildren<NeutronBehaviour>(); //* pega todas as instâncias que herdam de NeutronBehaviour
            if (childs.Length > 0)
            {
                //* Percorre as instâncias e pega os metódos e armazena no dicionário iRPC'S.
                for (int c = 0; c < childs.Length; c++)
                {
                    NeutronBehaviour child = childs[c];

                    #region Add Instances
                    if (!Childs.ContainsKey(child.ID))
                        Childs.Add(child.ID, child); //* Adiciona a instância no dict, para manter rápido acesso a qualquer instância.
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
                                    iRPCs.Add(key, new RPC(child, methods.Item2, method)); //* Adiciona o método no Dict, e monta sua estrutura RPC.
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