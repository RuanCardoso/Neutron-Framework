using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Server.Internal;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NeutronNetwork.Internal
{
    ///* Esta classe é a base do objeto de rede(NeutronView).
    public class ViewBehaviour : MonoBehaviour
    {
        #region Primitives
        /// <summary>
        ///* Este ID é usado para identificar a instância que irá invocar os iRPC'S.
        /// </summary>
        [ReadOnly] public int ID;
        /// <summary>
        ///* Define o ambiente que o objeto deve ser criado, Client, Server ou ambos.
        /// </summary>
        public Side Ambient = Side.Both;
        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do servidor.
        /// </summary>
        [NonSerialized] public bool IsServer;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna se o objeto é um objeto de cena, falso, se for um jogador.
        /// </summary>
        public bool IsSceneObject => SceneHelper.IsSceneObject(ID);
        #endregion

        #region Register
        /// <summary>
        ///* Retorna o jogador que é dono do objeto.
        /// </summary>
        [ReadOnly] public NeutronPlayer Owner;
        /// <summary>
        ///* A instância de Neutron a qual este objeto pertence.
        /// </summary>
        [NonSerialized] public Neutron _;
        #endregion

        #region Collection
        ///* Aqui será armazenado todos os metódos marcado com o atributo iRPC.
        [NonSerialized] public Dictionary<(byte, byte), RPC> iRPCs = new Dictionary<(byte, byte), RPC>();
        [NonSerialized] public Dictionary<int, NeutronBehaviour> neutronBehaviours = new Dictionary<int, NeutronBehaviour>();
        [NonSerialized] public NeutronSafeQueue<Action> m_ActionsDispatcher = new NeutronSafeQueue<Action>();
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
            try
            {
                for (int i = 0; i < 100 && m_ActionsDispatcher.Count > 0; i++)
                {
                    if (m_ActionsDispatcher.TryDequeue(out Action action))
                        action.Invoke();
                    else Debug.LogError("TryDequeue!");
                }
            }
            catch (Exception ex)
            {
                LogHelper.StackTrace(ex);
            }

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

        [ThreadSafe]
        public void Dispatch(Action action)
        {
            m_ActionsDispatcher.Enqueue(action);
        }

        #region Virtual Methods
        /// <summary>
        ///* Este método é chamado quando o objeto é instanciado e está pronto para uso, não tenho certeza, depois vejo.
        /// </summary>
        public virtual void OnNeutronStart() { }
        /// <summary>
        ///* Este método é chamado quando o objeto é registrado na rede, não tenho certeza, depois vejo.
        /// </summary>
        public virtual void OnNeutronAwake() { }
        #endregion

        #region Reflection
        //* Este método usa reflexão(Lento? é, mas só uma vez, foda-se hehehehhe), é chamado apenas uma vez quando o objeto é instanciado.
        private void GetIRPCS()
        {
            var neutronBehaviours = GetComponentsInChildren<NeutronBehaviour>(); //* pega todas as instâncias que herdam de NeutronBehaviour
            if (neutronBehaviours != null)
            {
                //* Percorre as instâncias e pega os metódos e armazena no dicionário iRPC'S.
                for (int i = 0; i < neutronBehaviours.Length; i++)
                {
                    NeutronBehaviour mInstance = neutronBehaviours[i];
                    if (mInstance != null)
                    {
                        #region Add Instances
                        this.neutronBehaviours.Add(mInstance.ID, mInstance); //* Adiciona a instância no dict, para manter rápido acesso a qualquer instância.
                        #endregion

                        #region Create RPC
                        var mType = mInstance.GetType();
                        MethodInfo[] mInfos = mType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int y = 0; y < mInfos.Length; y++) //* Percorre a parada.
                        {
                            iRPC[] Attrs = mInfos[y].GetCustomAttributes<iRPC>().ToArray(); //* Isso aí mesmo, pega todos os attributos iRPC, caso tenha mais de um.
                            if (Attrs != null)
                            {
                                foreach (iRPC iRPC in Attrs)
                                {
                                    (byte, byte) uniqueID = (iRPC.ID, mInstance.ID); //* Gera um ID para o metódo.
                                    if (!iRPCs.ContainsKey(uniqueID)) //* Verifica se não existe um metódo duplicado, ou seja, com o iRPC com mesmo ID de iRPC e ID de Instância, o ID do iRPC pode ser igual a de outro metódo se a instância(ID) for diferente.
                                        iRPCs.Add(uniqueID, new RPC(mInstance, mInfos[y], iRPC)); //* Adiciona o método no Dict, e monta sua estrutura RPC.
                                    else
                                        LogHelper.Error($"Duplicate ID not allowed in \"{mType.Name}\".");
                                }
                            }
                            else continue;
                        }
                        #endregion
                    }
                    else continue;
                }
            }
            else LogHelper.Error("Could not find any implementation of \"NeutronBehaviour\"");
        }
        #endregion
    }
}