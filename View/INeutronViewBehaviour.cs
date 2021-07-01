using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    ///* Esta classe é a base do objeto de rede(NeutronView).
    public class NeutronViewBehaviour : MonoBehaviour
    {
        #region Primitives
        /// <summary>
        ///* Este ID é usado para identificar a instância que irá invocar os iRPC'S.
        /// </summary>
        [ID] public int ID;
        /// <summary>
        ///* Define o ambiente que o objeto deve ser criado, Client, Server ou ambos.
        /// </summary>
        public Ambient Ambient = Ambient.Both;
        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do servidor.
        /// </summary>
        [ReadOnly] public bool IsServer;
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
        [ReadOnly] public Player Owner;
        /// <summary>
        ///* A instância de Neutron a qual este objeto pertence.
        /// </summary>
        [NonSerialized] public Neutron _;
        #endregion

        #region Collection
        ///* Aqui será armazenado todos os metódos marcado com o atributo iRPC.
        [NonSerialized] public Dictionary<int, RemoteProceduralCall> iRPCs = new Dictionary<int, RemoteProceduralCall>();
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

        #region MonoBehaviour
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
                        if (!NeutronLogger.LoggerError("Child objects cannot have \"NeutronView\", because their parent already has one."))
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
                        var mType = mInstance.GetType();
                        MethodInfo[] mInfos = mType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int y = 0; y < mInfos.Length; y++) //* Percorre a parada.
                        {
                            iRPC[] Attrs = mInfos[y].GetCustomAttributes<iRPC>().ToArray(); //* Isso aí mesmo, pega todos os attributos iRPC, caso tenha mais de um.
                            if (Attrs != null)
                            {
                                foreach (iRPC Attr in Attrs)
                                {
                                    int uniqueID = Attr.ID ^ mInstance.ID; //* Gera um ID para o metódo.
                                    if (!iRPCs.ContainsKey(uniqueID)) //* Verifica se não existe um metódo duplicado, ou seja, com o iRPC com mesmo ID de iRPC e ID de Instância, o ID do iRPC pode ser igual a de outro metódo se a instância(ID) for diferente.
                                        iRPCs.Add(uniqueID, new RemoteProceduralCall(mInstance, mInfos[y], Attr)); //* Adiciona o método no Dict, e monta sua estrutura.
                                    else
                                        NeutronLogger.Print($"Duplicate ID not allowed in \"{mType.Name}\".");
                                }
                            }
                            else continue;
                        }
                    }
                    else continue;
                }
            }
            else NeutronLogger.Print("Could not find any implementation of \"NeutronBehaviour\"");
        }
        #endregion
    }
}