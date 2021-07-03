using System.Collections;
using NeutronNetwork.Naughty.Attributes;
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
            Initialized = true; //* Define que está pronto para uso, antes disso, tudo falhará.
            {
                StartCoroutine(OnNeutronSerializeView());
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

        #region Default Options
        [SerializeField] [Separator] private bool m_ShowDefaultOptions = true;

        [Header("[Default Options]")] //* Opçoes padrão para classes com RPC único.

        [Range(NeutronConstants.MIN_SEND_RATE, NeutronConstants.MAX_SEND_RATE)]
        [SerializeField]/* [AllowNesting] */[ShowIf("m_ShowDefaultOptions")] protected int m_SendRate = 15; //* Quantidade de sincronizações por segundo.
        [SerializeField]/* [AllowNesting] */[ShowIf("m_ShowDefaultOptions")] protected SendTo m_SendTo = SendTo.Others;
        [SerializeField]/* [AllowNesting] */[ShowIf("m_ShowDefaultOptions")] protected Broadcast m_BroadcastTo = Broadcast.Server;
        [SerializeField]/* [AllowNesting] */[ShowIf("m_ShowDefaultOptions")] protected CacheMode m_CacheMode = CacheMode.None;
        [SerializeField]/* [AllowNesting] */[ShowIf("m_ShowDefaultOptions")] protected Protocol m_ReceivingProtocol = Protocol.Tcp;
        [SerializeField]/* [AllowNesting] */[ShowIf("m_ShowDefaultOptions")] [Separator] protected Protocol m_SendingProtocol = Protocol.Tcp;
        #endregion

        #region MonoBehaviour
        public virtual void Awake()
        { }

        public virtual void Start()
        { }

        public virtual void Update()
        {
            if (Initialized)
                OnNeutronUpdate();
        }

        public virtual void FixedUpdate()
        {
            if (Initialized)
                OnNeutronFixedUpdate();
        }

        public virtual void LateUpdate()
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
        /// <param name="nSendProtocol">* O protocolo que será usado para enviar os dados.</param>
        protected void iRPC(int nIRPCId, NeutronWriter nParameters, CacheMode nCacheMode, SendTo nSendTo, Broadcast nBroadcast, Protocol nRecProtocol, Protocol nSendProtocol)
        {
            int uniqueID = nIRPCId ^ ID;
            if (IsClient)
                NeutronView._.iRPC(NeutronView.ID, uniqueID, nParameters, nCacheMode, nSendTo, nBroadcast, nRecProtocol, nSendProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(NeutronView.ID, uniqueID, nParameters, NeutronView.Owner, nCacheMode, nSendTo, nBroadcast, nRecProtocol, nSendProtocol);
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
        /// <param name="nSendProtocol">* O protocolo que será usado para enviar os dados.</param>
        protected void iRPC(Player nPlayer, int nIRPCId, NeutronWriter nParameters, CacheMode nCacheMode, SendTo nSendTo, Broadcast nBroadcast, Protocol nRecProtocol, Protocol nSendProtocol)
        {
            int uniqueID = nIRPCId ^ ID;
            if (IsClient)
                NeutronView._.iRPC(nPlayer.ID, uniqueID, nParameters, nCacheMode, nSendTo, nBroadcast, nRecProtocol, nSendProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(nPlayer.ID, uniqueID, nParameters, NeutronView.Owner, nCacheMode, nSendTo, nBroadcast, nRecProtocol, nSendProtocol);
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
        /// <param name="nSendProtocol">* O protocolo que será usado para enviar os dados.</param>
        protected void iRPC(NeutronView nView, int nIRPCId, NeutronWriter nParameters, CacheMode nCacheMode, SendTo nSendTo, Broadcast nBroadcast, Protocol nRecProtocol, Protocol nSendProtocol)
        {
            int uniqueID = nIRPCId ^ ID;
            if (IsClient)
                NeutronView._.iRPC(nView.ID, uniqueID, nParameters, nCacheMode, nSendTo, nBroadcast, nRecProtocol, nSendProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(nView.ID, uniqueID, nParameters, NeutronView.Owner, nCacheMode, nSendTo, nBroadcast, nRecProtocol, nSendProtocol);
        }
        #endregion

        #region Network Serialize View
        private IEnumerator OnNeutronSerializeView()
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
                        l_IsOverriden = l_Method.DeclaringType != typeof(NeutronBehaviour); //* Define se OnNeutronSerializedView está implementado.
                }
            }
            #endregion

            #region Send To Network
            while (l_IsOverriden && HasAuthority)
            {
                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0); //* Limpa o escritor.
                    using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                    {
                        if (OnNeutronSerializeView(nWriter, nReader, true)) //* Invoca o metódo.
                        {
                            if (IsClient)
                                NeutronView._.Send(nWriter, NeutronView, ID, m_SendTo, m_BroadcastTo, m_ReceivingProtocol, m_SendingProtocol); //* Envia para a rede. Client->Server
                            else Neutron.Server.OnSerializeView(NeutronView.Owner, NeutronView, ID, nWriter, m_SendTo, m_BroadcastTo, m_ReceivingProtocol); //* Envia para a rede. Server->Client
                        }
                    }
                }
                yield return new WaitForSeconds(NeutronConstants.ONE_PER_SECOND / m_SendRate); //* SendRate, envios por segundo.
            }
            #endregion
        }
        /// <summary>
        ///* Usado para personalizar a sincronização de variáveis ​​em um script monitorado por um NeutronView.<br/>
        ///* É determinado automaticamente se as variáveis ​​que estão sendo serializadas devem ser enviadas ou recebidas.<br/>
        ///* O Metódo Dispose() é chamado automaticamente, não é necessário o uso da instrução "Using".
        /// </summary>
        /// <param name="nWriter">* Fluxo usado para escrever os parâmetros a serem enviados.</param>
        /// <param name="nReader">* Fluxo usado para ler os parâmetros recebidos.</param>
        /// <param name="isWriting">* Define se você está escrevendo ou lendo os dados.</param>
        /// <returns></returns>
        public virtual bool OnNeutronSerializeView(NeutronWriter nWriter, NeutronReader nReader, bool isWriting) => false;
        #endregion
    }
}