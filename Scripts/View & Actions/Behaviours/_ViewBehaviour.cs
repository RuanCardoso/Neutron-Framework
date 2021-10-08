using NeutronNetwork.Helpers;
using NeutronNetwork.Extensions;
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
        //* Preguiça de transformar isto aqui em propriedade, deixa assim por enquanto...
        #region Fields
        /// <summary>
        ///* Este ID é usado para identificar a instância que irá invocar os iRPC'S.
        /// </summary>
        [SerializeField] private int _id;
        /// <summary>
        ///* Define o ambiente que o objeto deve ser criado, Client, Server ou ambos.
        /// </summary>
        [SerializeField]
        private Side _side = Side.Both;
        /// <summary>
        ///* Define se o objeto deve ser destruído automaticamente.
        /// </summary>
        [SerializeField]
        private bool _autoDestroy = true;
        /// <summary>
        ///* Retorna o jogador que é dono do objeto.
        /// </summary>
        [SerializeField]
        [InfoBox("The properties of the owner of this network object.")]
        private NeutronPlayer _owner;
        #endregion

        #region Properties
        /// <summary>
        ///*Id do objeto de rede.
        /// </summary>
        public int Id {
            get => _id;
            protected set => _id = value;
        }

        /// <summary>
        ///* A instância de Neutron a qual este objeto pertence.
        /// </summary>
        public Neutron This {
            get;
            protected set;
        }

        /// <summary>
        ///* Retorna o dono deste objeto.
        /// </summary>
        public NeutronPlayer Owner {
            get => _owner;
            protected set => _owner = value;
        }

        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do servidor.
        /// </summary>
        public bool IsServer {
            get;
            protected set;
        }

        /// <summary>
        ///* Retorna se o objeto será destruído quando seu dono for desconectado ou sair do matchmaking.
        /// </summary>
        public bool AutoDestroy {
            get => _autoDestroy;
            set => _autoDestroy = value;
        }

        /// <summary>
        ///* Retorna o lado que o objeto de rede será instanciado.
        /// </summary>
        public Side Side => _side;

        /// <summary>
        ///* Retorna se o objeto é um objeto de cena.
        /// </summary>
        public bool IsSceneObject => RegisterMode == RegisterMode.Scene;

        /// <summary>
        ///* O tipo de registro usado para o objeto.
        /// </summary>
        public RegisterMode RegisterMode {
            get;
            set;
        }

        /// <summary>
        ///* O Transform anexado a este GameObject. 
        /// </summary>
        public Transform Transform {
            get;
            set;
        }
        #endregion

        #region Collections
        ///* Aqui será armazenado todos os metódos marcado com o atributo iRPC.
        [NonSerialized] public Dictionary<(byte, byte), RPCInvoker> iRPCs = new Dictionary<(byte, byte), RPCInvoker>();
        [NonSerialized] public Dictionary<int, NeutronBehaviour> NeutronBehaviours = new Dictionary<int, NeutronBehaviour>();
        #endregion

        #region Mono Behaviour
        private void Start() => Transform = transform;

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
                if (SceneHelper.IsInScene(gameObject))
                {
                    var views = FindObjectsOfType<NeutronView>();
                    if (Id == 0)
                        Id = Helper.GetAvailableId(views, x => x.Id, short.MaxValue);
                    else
                    {
                        if (!(Id >= short.MaxValue))
                        {
                            int count = views.Count(x => x.Id == Id);
                            if (count > 1)
                                Reset();
                        }
                        else
                            LogHelper.Error("Max Neutron Views reached!");
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
        public virtual bool OnNeutronRegister(NeutronPlayer player, bool isServer, RegisterMode registerMode, Neutron instance, short dynamicId = 0) => true;
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é instanciado.
        public void MakeAttributes()
        {
            var childs = GetComponentsInChildren<NeutronBehaviour>(); //* pega todas as instâncias que herdam de NeutronBehaviour
            if (childs.Length > 0)
            {
                //* Percorre as instâncias e pega os metódos e armazena no dicionário iRPC'S.
                for (int c = 0; c < childs.Length; c++)
                {
                    NeutronBehaviour child = childs[c];

                    #region Add Instances
                    if (!NeutronBehaviours.ContainsKey(child.Id))
                        NeutronBehaviours.Add(child.Id, child); //* Adiciona a instância no dict, para manter rápido acesso a qualquer instância.
                    else
                        LogHelper.Error($"Duplicate \"NeutronBehaviour\" ID not allowed in \"{child.GetType().Name}\". {child.Id}");
                    #endregion

                    if (child != null && child.enabled)
                    {
                        (iRPC[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<iRPC>(child);
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            (iRPC[], MethodInfo) methods = multiplesMethods[i];
                            for (int ii = 0; ii < methods.Item1.Count(); ii++)
                            {
                                iRPC method = methods.Item1[ii];
                                (byte, byte) key = (method.ID, child.Id);
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