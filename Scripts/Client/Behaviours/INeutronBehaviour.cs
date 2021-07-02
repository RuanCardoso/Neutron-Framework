using System.Collections;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;

namespace NeutronNetwork
{
    //* Classe base de todos os objetos.
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_BEHAVIOUR)]
    public class NeutronBehaviour : MonoBehaviour
    {
        #region Identity
        [Header("[Identity]")]
        [SerializeField] [ID] private int m_ID;
        //* Define de quem é a autoridade do objeto.
        [SerializeField] [Separator] private AuthorityMode m_Authority = AuthorityMode.Owner;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna se o objeto está pronto para uso.
        /// </summary>
        /// <value></value>
        private bool Initialized { get; set; }
        /// <summary>
        ///* ID que será usado para identificar a instância que deve invocar os iRPC's
        /// </summary>
        /// <value></value>
        public int ID => m_ID;
        /// <summary>
        ///* Retorna o tipo de autoridade usado.
        /// </summary>
        public AuthorityMode Authority => m_Authority;
        /// <summary>
        ///* Retorna o seu objeto de rede.
        /// </summary>
        /// <value></value>
        public NeutronView NeutronView { get; set; }
        #endregion

        #region Virtual Methods
        /// <summary>
        ///* É Seguro para chamadas internas.(IsMine, HasAuthority, IsServer).
        /// </summary>
        public virtual void OnNeutronStart()
        {
            Initialized = true;
            {
                StartCoroutine(IENeutronSerializeView());
            }
        }
        /// <summary>
        ///* É Seguro para chamadas internas.(IsMine, HasAuthority, IsServer).
        /// </summary>
        protected virtual void OnNeutronUpdate() { }
        /// <summary>
        ///* É Seguro para chamadas internas.(IsMine, HasAuthority, IsServer).
        /// </summary>
        protected virtual void OnNeutronFixedUpdate() { }
        /// <summary>
        ///* É Seguro para chamadas internas.(IsMine, HasAuthority, IsServer).
        /// </summary>
        protected virtual void OnNeutronLateUpdate() { }
        #endregion

        #region Extended Properties
        /// <summary>
        ///* Retorna se o objeto é seu, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsMine => IsClient && NeutronView._.IsMine(NeutronView.Owner);
        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do servidor, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsServer => NeutronView.IsServer;
        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do cliente, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsClient => !IsServer;
        /// <summary>
        ///* Retorna se você tem autoridade para controlar este objeto, uma versão simplificada de IsMine|IsServer|IsClient|IsMasterClient.
        /// </summary>
        /// <value></value>
        protected bool HasAuthority
        {
            get
            {
                if (NeutronView != null)
                {
                    switch (Authority)
                    {
                        case AuthorityMode.Owner:
                            return IsMine;
                        case AuthorityMode.Server:
                            return IsServer;
                        case AuthorityMode.OwnerAndServer:
                            return IsServer || IsMine;
                        case AuthorityMode.MasterClient:
                            return !IsServer && NeutronView._.IsMasterClient();
                        case AuthorityMode.IgnoreExceptServer:
                            return !IsServer;
                        case AuthorityMode.Ignore:
                            return true;
                        default:
                            return false;
                    }
                }
                else return NeutronLogger.LoggerError("Unable to find Neutron View");
            }
        }
        #endregion

        #region MonoBehaviour
        public void Awake()
        { }

        public void Start()
        { }

        public void Update()
        {
            if (Initialized)
                OnNeutronUpdate();
        }

        public void FixedUpdate()
        {
            if (Initialized)
                OnNeutronFixedUpdate();
        }

        public void LateUpdate()
        {
            if (Initialized)
                OnNeutronLateUpdate();
        }
        #endregion

        #region Neutron
        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        /// </summary>
        /// <param name="nIRPCId">* ID do metódo que será invocado.</param>
        /// <param name="nParameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nCacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="nSendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="nProtocol">* O protocolo que será usado para enviar os dados.</param>
        protected void iRPC(int nIRPCId, NeutronWriter nParameters, CacheMode nCacheMode, SendTo nSendTo, Broadcast nBroadcast, Protocol nProtocol)
        {
            int uniqueID = nIRPCId ^ ID;
            if (IsClient)
                NeutronView._.iRPC(NeutronView.ID, uniqueID, nParameters, nCacheMode, nSendTo, nBroadcast, nProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(NeutronView.ID, uniqueID, nParameters, NeutronView.Owner, nCacheMode, nSendTo, nBroadcast, nProtocol);
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* Envie o iRPC para um jogador específico, suporta o roteamento dos dados. 
        /// </summary>
        /// <param name="nPlayer">* O jogador de destino da mensagem.</param>
        /// <param name="nIRPCId">* ID do metódo que será invocado.</param>
        /// <param name="nParameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nCacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="nSendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="nProtocol">* O protocolo que será usado para enviar os dados.</param>
        protected void iRPC(Player nPlayer, int nIRPCId, NeutronWriter nParameters, CacheMode nCacheMode, SendTo nSendTo, Broadcast nBroadcast, Protocol nProtocol)
        {
            int uniqueID = nIRPCId ^ ID;
            if (IsClient)
                NeutronView._.iRPC(nPlayer.ID, uniqueID, nParameters, nCacheMode, nSendTo, nBroadcast, nProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(nPlayer.ID, uniqueID, nParameters, NeutronView.Owner, nCacheMode, nSendTo, nBroadcast, nProtocol);
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* Envie o iRPC para um objeto de rede específico, suporta o roteamento dos dados. 
        /// </summary>
        /// <param name="nView">* O objeto de rede de destino da mensagem.</param>
        /// <param name="nIRPCId">* ID do metódo que será invocado.</param>
        /// <param name="nParameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nCacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="nSendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="nProtocol">* O protocolo que será usado para enviar os dados.</param>
        protected void iRPC(NeutronView nView, int nIRPCId, NeutronWriter nParameters, CacheMode nCacheMode, SendTo nSendTo, Broadcast nBroadcast, Protocol nProtocol)
        {
            int uniqueID = nIRPCId ^ ID;
            if (IsClient)
                NeutronView._.iRPC(nView.ID, uniqueID, nParameters, nCacheMode, nSendTo, nBroadcast, nProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(nView.ID, uniqueID, nParameters, NeutronView.Owner, nCacheMode, nSendTo, nBroadcast, nProtocol);
        }
        #endregion

        #region Network Serialize View
        private IEnumerator IENeutronSerializeView()
        {
            #region Reflection
            bool l_IsOverriden = false;
            NeutronBehaviour l_Instance = this;
            if (l_Instance != null)
            {
                if (!l_IsOverriden)
                {
                    var l_Method = l_Instance.GetType().GetMethod("OnNeutronSerializeView", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    if (l_Method != null)
                        l_IsOverriden = l_Method.DeclaringType != typeof(NeutronBehaviour);
                }
            }
            #endregion

            #region Send
            while (true)
            {
                if (Initialized && l_IsOverriden && HasAuthority)
                {
                    using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                    {
                        nWriter.SetLength(0);
                        using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                        {
                            if (OnNeutronSerializeView(nWriter, nReader, true))
                            {
                                NeutronView._.Send(nWriter, NeutronView, ID, Protocol.Udp, Protocol.Udp);
                            }
                        }
                    }
                }
                yield return new WaitForSeconds(0.01f);
            }
            #endregion
        }

        public virtual bool OnNeutronSerializeView(NeutronWriter nWriter, NeutronReader nReader, bool isWriting) => false;
        #endregion
    }
}