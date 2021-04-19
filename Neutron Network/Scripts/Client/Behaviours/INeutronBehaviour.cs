using NeutronNetwork.Internal.Attributes;
using UnityEngine;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_BEHAVIOUR)]
    public class NeutronBehaviour : MonoBehaviour
    {
        #region Identity
        [Header("[Identity]")]
        [SerializeField] [ID] [DisableField] private int iD;
        [SerializeField] [Separator] private AuthorityMode authority = AuthorityMode.Owner;
        #endregion

        #region Properties
        /// <summary>
        /// <para>PT: Este ID serve para identificar a instância que deve executar o método marcado com o atributo dinâmico.</para>
        /// <para>EN: This ID is used to identify the instance that should execute the method marked with the dynamic attribute.</para> 
        /// </summary>
        /// <value>Unique ID</value>
        public int ID => iD;
        /// <summary>
        /// </summary>
        /// <value>
        /// <para>PT: Retorna um valor que indica o tipo de autoridade usado.</para><br/>
        /// <para>EN: Returns a value indicating the type of authority used.</para>
        /// </value>
        protected AuthorityMode Authority => authority;
        /// <summary>
        /// <para>PT: Indica se a instancia está inicializada e pronta para uso.</para>
        /// <para>EN: Indicates whether the instance is initialized and ready for use.</para>
        /// </summary>
        /// <value></value>
        private bool Initialized { get; set; }
        /// <summary>
        /// <para>PT: Componente NeutronView que é usado para se comunicar e identificar cada objeto na rede.</para>
        /// <para>EN: NeutronView component that is used to communicate and identify each object on the network.</para>
        /// </summary>
        /// <value></value>
        public NeutronView NeutronView { get; set; }
        #endregion

        #region Virtual Methods
        /// <summary>
        /// <para>PT: O Mesmo que o Start(), só que seguro para chamadas internas.(IsMine, HasAuthority, IsServer, IsBot).</para>
        /// <para>EN: The same as Start(), only safe for internal calls. (IsMine, HasAuthority, IsServer, IsBot).</para>
        /// </summary>
        public virtual void OnNeutronStart() => Initialized = true;
        /// <summary>
        /// <para>PT: O Mesmo que o Update(), só que seguro para chamadas internas.(IsMine, HasAuthority, IsServer, IsBot).</para>
        /// <para>EN: The same as Update(), only safe for internal calls. (IsMine, HasAuthority, IsServer, IsBot).</para>
        /// </summary>
        protected virtual void OnNeutronUpdate() { }
        /// <summary>
        /// <para>PT: O Mesmo que o FixedUpdate(), só que seguro para chamadas internas.(IsMine, HasAuthority, IsServer, IsBot).</para>
        /// <para>EN: The same as FixedUpdate(), only safe for internal calls. (IsMine, HasAuthority, IsServer, IsBot).</para>
        /// </summary>
        protected virtual void OnNeutronFixedUpdate() { }
        #endregion

        #region Extended Properties
        /// <summary>
        /// <para>PT: Verifica se o objeto é seu, HasAuthority também pode ser usado para verificar se o objeto é meu.</para>
        /// <para>EN: Check if the object is yours, HasAuthority can also be used to check if the object is mine.</para>
        /// </summary>
        /// <returns></returns>
        protected bool IsMine => Initialized && Mine();
        /// <summary>
        /// Ignore this/Ignore isto porra.
        /// </summary>
        protected bool IsBot => Initialized && NeutronView.owner.IsBot;
        /// <summary>
        /// <para>PT: Verifica se é o objeto do servidor.</para>
        /// <para>EN: Checks if it is the server object.</para>
        /// </summary>
        protected bool IsServer => Initialized && NeutronView.isServer;
        /// <summary>
        /// <para>PT: Verifica se é o objeto do cliente.</para>
        /// <para>EN: Checks if it is the client object.</para>
        /// </summary>
        protected bool IsClient => Initialized && !NeutronView.isServer;
        /// <summary>
        /// <para>PT: Verifica se você tem autoridade para controlar este objeto, uma versão simplificada de IsMine / IsServer / IsClient / IsMasterClient.</para>
        /// <para>EN: Checks whether you have the authority to control this object, a simplified version of IsMine / IsServer / IsClient / IsMasterClient.</para>
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
                            return MasterClient();
                        case AuthorityMode.IgnoreExceptServer:
                            return !IsServer;
                        case AuthorityMode.Ignore:
                            return true;
                        default:
                            return false;
                    }
                }
                else return NeutronUtils.LoggerError("Unable to find Neutron View");
            }
        }
        #endregion

        #region MonoBehaviour
        public void Awake()
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
        #endregion

        #region Neutron
        /// <summary>
        /// <para>PT: Verifica se o objeto é meu.</para>
        /// <para>EN: Check if the object is mine.</para>
        /// </summary>
        /// <returns></returns>
        private bool Mine()
        {
            return !IsServer && NeutronView._.IsMine(NeutronView.owner);
        }
        /// <summary>
        /// <para>PT: Verifica se você é o MasterClient.</para>
        /// <para>EN: Check if you are the MasterClient.</para>
        /// </summary>
        /// <returns></returns>
        private bool MasterClient()
        {
            return !IsServer && NeutronView._.IsMasterClient();
        }
        /// <summary>
        /// <para>PT: Invoca o método na rede.</para>
        /// <para>EN: Invokes the method on the network.</para>
        /// </summary>
        /// <param name="DynamicID">
        /// <para>PT: ID do método que será invocado.</para><br/>
        /// <para>EN: ID of the method that will be invoked.</para>
        /// </param>
        /// <param name="cacheMode">
        /// <para>PT: Indica se o método será armazenado em cache para se possa obter depois.</para><br/>
        /// <para>EN: Indicates whether the method will be cached so that it can be obtained later.</para>
        /// </param>
        /// <param name="parameters">
        /// <para>PT: Escreva os parâmetros que serão enviados pela rede.</para><br/>
        /// <para>EN: Write the parameters that will be sent over the network.</para>
        /// </param>
        /// <param name="sendTo">
        /// <para>PT: Para quem estes dados serão enviados?</para><br/>
        /// <para>EN: To whom will this data be sent?</para>
        /// </param>
        /// <param name="broadcast">
        /// <para>PT: Onde estes dados serão enviados?</para><br/>
        /// <para>EN: Where will this data be sent?</para>
        /// </param>
        /// <param name="protocol">
        /// <para>PT: Qual protocolo será usado para enviar estes dados?</para><br/>
        /// <para>EN: Which protocol will be used to send this data?</para>
        /// </param>
        protected void Dynamic(int DynamicID, NeutronWriter parameters, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            int uniqueID = DynamicID ^ ID;
            if (IsClient)
                NeutronView._.Dynamic(NeutronView.ID, uniqueID, parameters, cacheMode, sendTo, broadcast, protocol);
            else if (IsServer)
                Neutron.Server.Dynamic(NeutronView.ID, uniqueID, parameters, NeutronView.owner, cacheMode, sendTo, broadcast, protocol);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkID"></param>
        /// <param name="DynamicID"></param>
        /// <param name="cacheMode"></param>
        /// <param name="parameters"></param>
        /// <param name="sendTo"></param>
        /// <param name="broadcast"></param>
        /// <param name="protocol"></param>
        protected void Dynamic(int networkID, int DynamicID, NeutronWriter parameters, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            int uniqueID = DynamicID ^ ID;
            if (IsClient)
                NeutronView._.Dynamic(networkID, uniqueID, parameters, cacheMode, sendTo, broadcast, protocol);
            else if (IsServer)
                Neutron.Server.Dynamic(networkID, uniqueID, parameters, NeutronView.owner, cacheMode, sendTo, broadcast, protocol);
        }
        #endregion
    }
}