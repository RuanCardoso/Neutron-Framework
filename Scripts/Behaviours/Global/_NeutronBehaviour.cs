using System.Collections;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;
using System;
using NeutronNetwork.Server.Internal;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    //* Classe base de todos os objetos.
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_BEHAVIOUR)]
    public class NeutronBehaviour : MonoBehaviour
    {
        #region Fields -> Inspector
        [Header("[Identity]")]
        [SerializeField] [ReadOnly] private byte _iD;
        [SerializeField] [HorizontalLineDown] private Authoritys _authority = Authoritys.Mine;
        [HideInInspector]
        [SerializeField] private bool _hasOnAutoSynchronization, _hasIRPC;
        #endregion

        #region Fields
        /// <summary>
        ///* Retorna se o objeto está pronto para uso.
        /// </summary>
        /// <value></value>
        private bool _isInitialized;
        #endregion

        #region Properties
        /// <summary>
        ///* ID que será usado para identificar a instância que deve invocar os iRPC's
        /// </summary>
        /// <value></value>
        public byte ID => _iD;
        /// <summary>
        ///* Retorna o nível de autoridade usado.
        /// </summary>
        public Authoritys Authority => _authority;
        /// <summary>
        ///* Retorna se o objeto é seu, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsMine => IsClient && NeutronView.This.IsMine(NeutronView.Player);
        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        /// </summary>
        /// <returns></returns>
        protected bool IsMasterClient => IsClient && NeutronView.This.IsMasterClient();
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
        ///* Retorna se você tem autoridade para controlar este objeto, uma versão simplificada de IsMine|IsServer|IsClient|IsMasterClient.<br/>
        ///* Retorna com base no tipo de Autoridade escolhido no inspetor.
        /// </summary>
        /// <value></value>
        protected bool HasAuthority {
            get {
                switch (Authority)
                {
                    case Authoritys.Mine:
                        {
                            return IsMine;
                        }
                    case Authoritys.Server:
                        {
                            return IsServer;
                        }
                    case Authoritys.Master:
                        {
                            return IsMasterClient;
                        }
                    case Authoritys.Mine | Authoritys.Server:
                        {
                            return IsMine || IsServer;
                        }
                    case Authoritys.Mine | Authoritys.Master:
                        {
                            return IsMine || IsMasterClient;
                        }
                    case Authoritys.All:
                        {
                            return true;
                        }
                    case Authoritys.Custom:
                        {
                            return OnCustomAuthority();
                        }
                    default:
                        return LogHelper.Error("Authority not implemented!");
                }
            }
        }
        /// <summary>
        ///* Definido quando o servidor tem a autoridade sobre o objeto, isto é, impede que o servidor execute a sí mesmo alguma instrução que faz parte do iRPC ou OnSerializeNeutronView.<br/>
        ///* Se o Cliente possuir a autoridade sobre o objeto, retorna "True".
        /// </summary>
        protected bool DoNotPerformTheOperationOnTheServer => IsClient || Authority != Authoritys.Server;
        /// <summary>
        ///* Retorna o seu objeto de rede.
        /// </summary>
        /// <value></value>
        public NeutronView NeutronView { get; set; }
        #endregion

        #region Custom MonoBehaviour Methods
        /// <summary>
        ///* É Seguro para chamadas internas.(IsMine, HasAuthority, IsServer).
        /// </summary>
        public virtual void OnNeutronStart()
        {
            if (NeutronView != null)
            {
                _isInitialized = true; //* Define que está pronto para uso, antes disso, tudo falhará.
                if (_isInitialized)
                    StartCoroutine(InitializeAutoSync());
            }
            else
                LogHelper.Error("\"Neutron View\" object not found, failed to instantiate in network.");
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

        #region Collections
        [SerializeField] [ShowIf("_hasIRPC")] private List<NeutronDataSyncOptions> _syncOptions = new List<NeutronDataSyncOptions>();
        [SerializeField] [HorizontalLineDown] [ShowIf("_hasOnAutoSynchronization")] private NeutronDataSyncOptionsWithRate OnAutoSynchronizationOptions;
        private Dictionary<int, NeutronDataSyncOptions> _runtimeSyncOptions;
        #endregion

        #region MonoBehaviour
        public virtual void Awake()
        {
            _runtimeSyncOptions = _syncOptions.ToDictionary(x => x.RpcId);
        }

        public virtual void Start()
        { }

        public virtual void Update()
        {
            if (_isInitialized)
                OnNeutronUpdate();
        }

        public virtual void FixedUpdate()
        {
            if (_isInitialized)
                OnNeutronFixedUpdate();
        }

        public virtual void LateUpdate()
        {
            if (_isInitialized)
                OnNeutronLateUpdate();
        }

        public void Reset()
        {
#if UNITY_EDITOR
            _iD = 0;
            OnValidate();
#endif
        }

        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && ID == 0)
            {
                NeutronBehaviour[] neutronBehaviours = transform.root.GetComponentsInChildren<NeutronBehaviour>();
                if (neutronBehaviours.Length <= byte.MaxValue)
                {
                    for (int i = 0; i < neutronBehaviours.Length; i++)
                    {
                        if (neutronBehaviours[i] == this)
                            _iD = (byte)(i + 1);
                        else if (neutronBehaviours[i].ID == ID)
                            neutronBehaviours[i].Reset();
                    }
                }
                else
                    LogHelper.Error("Only 255 instances of \"NeutronBehaviour\" can exist per network object(NeutronView).");
            }

            #region Reflection
            if (!Application.isPlaying)
            {
                NeutronBehaviour instance = this;
                if (instance != null)
                {
                    var method = ReflectionHelper.GetMethod("OnAutoSynchronization", instance);
                    if (method != null)
                        _hasOnAutoSynchronization = method.DeclaringType != typeof(NeutronBehaviour); //* Define se OnNeutronSerializedView está implementado.
                    else
                        _hasOnAutoSynchronization = false;

                    iRPC[][] multiplesMethods = ReflectionHelper.GetMultipleAttributes<iRPC>(instance);
                    _hasIRPC = multiplesMethods.Length > 0;
                    if (_hasIRPC && _syncOptions != null)
                    {
                        List<int> listOfId = new List<int>();
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            iRPC[] iRPCs = multiplesMethods[i];
                            for (int rI = 0; rI < iRPCs.Length; rI++)
                            {
                                iRPC iRPC = iRPCs[rI];
                                if (iRPC != null)
                                {
                                    listOfId.Add(iRPC.ID);

                                    var option = new NeutronDataSyncOptions();
                                    option.InstanceId = instance.ID;
                                    option.RpcId = iRPC.ID;

                                    if (!_syncOptions.Contains(option))
                                        _syncOptions.Add(option);
                                    else
                                        continue;
                                }
                                else
                                    continue;
                            }
                        }

                        for (int i = 0; i < _syncOptions.Count; i++)
                        {
                            var option = _syncOptions[i];
                            if (!listOfId.Contains(option.RpcId))
                                _syncOptions.Remove(option);
                        }

                        if (_syncOptions.Count > listOfId.Count)
                        {
                            int diff = _syncOptions.Count - listOfId.Count;
                            for (int i = 0; i < diff; i++)
                                _syncOptions.RemoveAt(_syncOptions.Count - 1);
                        }
                    }
                    else if (_syncOptions != null)
                        _syncOptions.Clear();
                }
            }
            #endregion
#endif
        }
        #endregion

        #region Neutron
        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nCacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="nSendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="nSendProtocol">* O protocolo que será usado para enviar os dados.</param>
#pragma warning disable IDE1006
        protected void iRPC(int id, NeutronWriter writer)
#pragma warning restore IDE1006
        {
            var Options = _runtimeSyncOptions[id];
            int uniqueID = id ^ ID;
            if (IsClient)
                NeutronView.This.iRPC(NeutronView.ID, uniqueID, writer, Options.Cache, Options.TergetTo, Options.TunnelingTo, Options.RecProtocol, Options.SendProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(NeutronView.ID, uniqueID, writer, NeutronView.Player, Options.Cache, Options.TergetTo, Options.TunnelingTo, Options.SendProtocol);
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* Envie o iRPC para um jogador específico, suporta o roteamento dos dados. 
        /// </summary>
        /// <param name="player">* O jogador de destino da mensagem.</param>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nCacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="nSendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="nSendProtocol">* O protocolo que será usado para enviar os dados.</param>
#pragma warning disable IDE1006
        protected void iRPC(int id, NeutronPlayer player, NeutronWriter writer)
#pragma warning restore IDE1006
        {
            var Options = _runtimeSyncOptions[id];
            int uniqueID = id ^ ID;
            if (IsClient)
                NeutronView.This.iRPC(player.ID, uniqueID, writer, Options.Cache, Options.TergetTo, Options.TunnelingTo, Options.RecProtocol, Options.SendProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(player.ID, uniqueID, writer, NeutronView.Player, Options.Cache, Options.TergetTo, Options.TunnelingTo, Options.SendProtocol);
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.<br/>
        ///* Envie o iRPC para um objeto de rede específico, suporta o roteamento dos dados. 
        /// </summary>
        /// <param name="view">* O objeto de rede de destino da mensagem.</param>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="nCacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="nSendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="nSendProtocol">* O protocolo que será usado para enviar os dados.</param>
#pragma warning disable IDE1006
        protected void iRPC(int id, NeutronView view, NeutronWriter writer)
#pragma warning restore IDE1006
        {
            var Options = _runtimeSyncOptions[id];
            int uniqueID = id ^ ID;
            if (IsClient)
                NeutronView.This.iRPC(view.ID, uniqueID, writer, Options.Cache, Options.TergetTo, Options.TunnelingTo, Options.RecProtocol, Options.SendProtocol);
            else if (IsServer)
                Neutron.Server.iRPC(view.ID, uniqueID, writer, NeutronView.Player, Options.Cache, Options.TergetTo, Options.TunnelingTo, Options.SendProtocol);
        }
        #endregion

        #region Virtual Methods
        private IEnumerator InitializeAutoSync()
        {
            var option = OnAutoSynchronizationOptions;
            while (_hasOnAutoSynchronization && HasAuthority)
            {
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                    {
                        if (OnAutoSynchronization(writer, reader, true)) //* Invoca o metódo.
                        {
                            if (IsClient)
                                NeutronView.This.OnAutoSynchronization(writer, NeutronView, ID, option.TargetTo, option.TunnelingTo, option.RecProtocol, option.SendProtocol); //* Envia para a rede. Client->Server
                            else
                                Neutron.Server.OnAutoSynchronization(NeutronView.Player, NeutronView, ID, writer, option.TargetTo, option.TunnelingTo, option.SendProtocol); //* Envia para a rede. Server->Client
                        }
                    }
                }
                yield return new WaitForSeconds(Settings.ONE_PER_SECOND / option.SendRate); //* SendRate, envios por segundo.
            }
        }
        /// <summary>
        ///* Usado para personalizar a sincronização de variáveis ​​em um script monitorado por um NeutronView.<br/>
        ///* É determinado automaticamente se as variáveis ​​que estão sendo serializadas devem ser enviadas ou recebidas.<br/>
        /// </summary>
        /// <param name="writer">* Fluxo usado para escrever os parâmetros a serem enviados.</param>
        /// <param name="reader">* Fluxo usado para ler os parâmetros recebidos.</param>
        /// <param name="isWriting">* Define se você está escrevendo ou lendo os dados.</param>
        public virtual bool OnAutoSynchronization(NeutronWriter writer, NeutronReader reader, bool isWriting) => OnValidateAutoSynchronization(isWriting);
        /// <summary>
        ///* Usado para validar OnAutoSynchronization ao lado do cliente ou servidor.
        /// </summary>
        /// <param name="isWriting">Se "True", Validação ocorre ao lado do Cliente, se "False", ocorre ao lado do Servidor.</param>
        /// <returns></returns>
        protected virtual bool OnValidateAutoSynchronization(bool isWriting) => true;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isWriting"></param>
        /// <returns></returns>
        protected virtual bool OnCustomAuthority() => false;
        #endregion
    }
}