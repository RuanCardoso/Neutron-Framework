using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    /// <summary>
    ///* Base de todos os objetos de rede, seja ao lado do servidor ou ao lado do cliente.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_BEHAVIOUR)]
    public class NeutronBehaviour : GlobalBehaviour
    {
        private readonly NeutronStream _packetStream = new NeutronStream();

        #region Fields -> Inspector
        [Header("[Identity]")]
        [SerializeField] private byte _id;
        [SerializeField] [ShowIf("_authority", AuthorityMode.Handled)] private NeutronBehaviour _authorityHandledBy;
        [SerializeField] [HorizontalLineDown] private AuthorityMode _authority = AuthorityMode.Mine;
        [HideInInspector]
        [SerializeField] private bool _hasOnAutoSynchronization, _hasIRPC;
        #endregion

        #region Fields
        private float _autoSyncTimeDelay;
        [SerializeField] [HorizontalLineDown] [ShowIf("_hasOnAutoSynchronization")] private AutoSyncOptions _onAutoSynchronizationOptions = new AutoSyncOptions();
        #endregion

        #region Properties
        /// <summary>
        ///* Id que será usado para identificar a instância que deve invocar os iRPC's.
        /// </summary>
        /// <value></value>
        public byte Id => _id;

        /// <summary>
        ///* Retorna o nível de autoridade usado.
        /// </summary>
        protected AuthorityMode Authority => _authority;

        /// <summary>
        ///* Define o nível de autoridade para o OnAutoSynchronization.
        /// </summary>
        protected virtual bool AutoSyncAuthority => HasAuthority;

        /// <summary>
        ///* Retorna se o objeto está registrado na rede.
        /// </summary>
        public bool IsRegistered {
            get;
            private set;
        }

        /// <summary>
        ///* Retorna se o objeto é seu, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsMine => IsRegistered && This.IsMine(Owner);

        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        /// </summary>
        /// <returns></returns>
        protected bool IsMasterClient => IsRegistered && Owner.IsMaster;

        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do servidor, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsServer => IsRegistered && NeutronView.IsServer;

        /// <summary>
        ///* Retorna se o objeto é o objeto do lado do cliente, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsClient => IsRegistered && !IsServer;

        /// <summary>
        ///* Retorna se o objeto é seu, HasAuthority é uma alternativa.
        /// </summary>
        /// <returns></returns>
        protected bool IsCustom => IsRegistered && OnCustomAuthority();

        /// <summary>
        ///* Retorna se você tem autoridade para controlar este objeto, uma versão simplificada de IsMine|IsServer|IsClient|IsMasterClient.<br/>
        ///* Retorna com base no tipo de Autoridade escolhido no inspetor.
        /// </summary>
        /// <value></value>
        protected bool HasAuthority {
            get {
                switch (Authority)
                {
                    case AuthorityMode.Mine:
                        {
                            return IsMine;
                        }
                    case AuthorityMode.Server:
                        {
                            return IsServer;
                        }
                    case AuthorityMode.Master:
                        {
                            return IsMasterClient;
                        }
                    case AuthorityMode.MineAndServer:
                        {
                            return IsMine || IsServer;
                        }
                    case AuthorityMode.MineAndMaster:
                        {
                            return IsMine || IsMasterClient;
                        }
                    case AuthorityMode.ServerAndMaster:
                        {
                            return IsServer || IsMasterClient;
                        }
                    case AuthorityMode.All:
                        {
                            return true;
                        }
                    case AuthorityMode.Custom:
                        {
                            return IsCustom;
                        }
                    case AuthorityMode.Handled:
                        {
                            return _authorityHandledBy != null && _authorityHandledBy.HasAuthority;
                        }
                    default:
                        return LogHelper.Error("Authority not implemented!");
                }
            }
        }

        /// <summary>
        ///* Definido quando o servidor tem a autoridade sobre o objeto, isto é, impede que o servidor execute a sí mesmo alguma instrução que faz parte do iRPC ou OnAutoSynchronization.<br/>
        ///* Se o Cliente possuir a autoridade sobre o objeto, retorna "True".
        /// </summary>
        protected bool DoNotPerformTheOperationOnTheServer => IsClient || Authority != AuthorityMode.Server;

        /// <summary>
        ///* Retorna o seu objeto de rede.
        /// </summary>
        /// <value></value>
        public NeutronView NeutronView {
            get;
            set;
        }

        /// <summary>
        ///* A instância de Neutron a qual este objeto pertence.
        /// </summary>
        protected Neutron This => NeutronView.This;

        /// <summary>
        ///* Retorna o jogador que é dono do objeto.
        /// </summary>
        protected NeutronPlayer Owner => NeutronView.Owner;

        /// <summary>
        ///* Obtém a cena a qual este objeto pertence.
        /// </summary>
        protected Scene Scene => gameObject.scene;

        /// <summary>
        ///* Obtém a cena de física 3D usada pela cena.
        /// </summary>
        protected PhysicsScene Physics3D => Scene.GetPhysicsScene();

        /// <summary>
        ///* Obtém a cena de física 2D usada pela cena.
        /// </summary>
        protected PhysicsScene2D Physics2D => Scene.GetPhysicsScene2D();

        /// <summary>
        ///* Obtenha o tempo atual da rede em segundos(sec).<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).
        /// </summary>
        protected double NetworkTime => This.NetworkTime.Time;

        /// <summary>
        ///* Obtenha o tempo atual em segundos(sec) desde do início da conexão.<br/>
        ///* Multiplique por mil para obter em milisegundos(ms).<br/>
        ///* Não afetado pela rede.
        /// </summary>
        protected double LocalTime => This.NetworkTime.LocalTime;

        /// <summary>
        ///* Obtém as opções do AutoSync definidas no inspetor.
        /// </summary>
        public AutoSyncOptions AutoSyncOptions => _onAutoSynchronizationOptions;
        #endregion

        #region Collections
        [SerializeField] [ShowIf("_hasIRPC")] [Label("iRpcOptions")] protected List<iRpcOptions> _iRpcOptions = new List<iRpcOptions>();
        [NonSerialized] protected readonly Dictionary<byte, iRpcOptions> RuntimeIRpcOptions = new Dictionary<byte, iRpcOptions>();
        #endregion

        #region Custom Mono Behaviour Methods
        /// <summary>
        ///* É Seguro para chamadas internas.(IsMine, HasAuthority, IsServer..etc).
        /// </summary>
        public virtual void OnNeutronStart()
        {
            if (_hasOnAutoSynchronization)
            {
                NeutronStream packetStream = GetPacketStream();
                if (packetStream == null && _onAutoSynchronizationOptions.FixedSize)
                    throw new Exception("AutoSync: Packet stream not implemented!");
                if (packetStream != null && !packetStream.IsFixedSize && _onAutoSynchronizationOptions.FixedSize)
                    LogHelper.Warn("AutoSync: The stream has no fixed size! performance is lower if you send with very frequency.");
            }

            //* Inicializa os iRpcs.
            foreach (iRpcOptions option in _iRpcOptions)
            {
                if (option.Instance.Id == Id)
                    RuntimeIRpcOptions.Add(option.RpcId, option);
                else
                    NeutronView.NeutronBehaviours[option.Instance.Id].RuntimeIRpcOptions.Add(option.RpcId, option);
            }
            //* Define que o metódo foi registrado.
            IsRegistered = true;
        }

        /// <summary>
        ///* Single Update, as atualizações de todos os objetos são chamados por um só invocador(Global).<br/>
        ///* É seguro para chamadas internas.(IsMine, HasAuthority, IsServer..etc).
        /// </summary>
        protected virtual void OnNeutronUpdate()
        {
            if (_hasOnAutoSynchronization)
            {
                _autoSyncTimeDelay -= Time.deltaTime;
                if (_autoSyncTimeDelay <= 0)
                {
                    NeutronStream packetStream = GetPacketStream();
                    if (_hasOnAutoSynchronization && AutoSyncAuthority)
                    {
                        if (!_onAutoSynchronizationOptions.FixedSize)
                        {
                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                            {
                                stream.Writer.SetPosition(PacketSize.AutoSync);
                                if (OnAutoSynchronization(stream, true))
                                    This.OnAutoSynchronization(stream, NeutronView, Id, _onAutoSynchronizationOptions.Protocol, IsServer); //* Envia para a rede.
                            }
                        }
                        else
                        {
                            packetStream.Writer.SetPosition(PacketSize.AutoSync);
                            if (OnAutoSynchronization(packetStream, true))
                                This.OnAutoSynchronization(packetStream, NeutronView, Id, _onAutoSynchronizationOptions.Protocol, IsServer); //* Envia para a rede.
                        }
                    }
                    _autoSyncTimeDelay = NeutronConstantsSettings.ONE_PER_SECOND / _onAutoSynchronizationOptions.PacketsPerSecond;
                }
            }
        }

        /// <summary>
        ///* Single Update, as atualizações de todos os objetos são chamados por um só invocador(Global).<br/>
        ///* É seguro para chamadas internas.(IsMine, HasAuthority, IsServer..etc).
        /// </summary>
        protected virtual void OnNeutronFixedUpdate() { }

        /// <summary>
        ///* Single Update, as atualizações de todos os objetos são chamados por um só invocador(Global).<br/>
        ///* É seguro para chamadas internas.(IsMine, HasAuthority, IsServer..etc).
        /// </summary>
        protected virtual void OnNeutronLateUpdate() { }
        #endregion

        #region Mono Behaviour
        protected virtual void Update()
        {
            if (IsRegistered)
                OnNeutronUpdate();
        }

        protected virtual void FixedUpdate()
        {
            if (IsRegistered)
                OnNeutronFixedUpdate();
        }

        protected virtual void LateUpdate()
        {
            if (IsRegistered)
                OnNeutronLateUpdate();
        }

        protected virtual void Reset()
        {
#if UNITY_EDITOR
            _id = 0;
            OnValidate();
#endif
        }

        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            LoadOptions();
#endif
        }

        //* Não entendo porra nenhuma que tá aqui, sim foi eu que fiz..... só sei que é pra attribuir o Id do view e os metódos rpc no dict.
        //* Chamado apenas no editor..
        private void LoadOptions()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var behaviours = transform.root.GetComponentsInChildren<NeutronBehaviour>();
                if (behaviours.Length <= byte.MaxValue)
                {
                    if (Id == 0)
                        _id = (byte)Helper.GetAvailableId(behaviours, x => x.Id, byte.MaxValue);
                    else
                    {
                        if (!(Id >= byte.MaxValue))
                        {
                            int count = behaviours.Count(x => x.Id == Id);
                            if (count > 1)
                                Reset();
                        }
                        else
                            LogHelper.Error("Max Neutron Behaviours reached in this Neutron View!");
                    }
                }
                else
                    throw new Exception("Only 255 instances of \"NeutronBehaviour\" can exist per network object(NeutronView).");
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

                    (iRPC[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<iRPC>(instance);
                    _hasIRPC = multiplesMethods.Length > 0;
                    if (_hasIRPC && _iRpcOptions != null)
                    {
                        List<byte> listOfId = new List<byte>();
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            (iRPC[], MethodInfo) iRPCs = multiplesMethods[i];
                            for (int rI = 0; rI < iRPCs.Item1.Length; rI++)
                            {
                                iRPC iRPC = iRPCs.Item1[rI];
                                var option = new iRpcOptions
                                {
                                    Instance = instance,
                                    OriginalInstance = instance,
                                    RpcId = iRPC.ID,
                                    Name = iRPCs.Item2.Name,
                                };
                                listOfId.Add(option.RpcId);
                                if (!_iRpcOptions.Contains(option))
                                    _iRpcOptions.Add(option);
                                else
                                    continue;
                            }
                        }

                        if (_iRpcOptions.Count > listOfId.Count)
                        {
                            _iRpcOptions.Where(x => !listOfId.Contains(x.RpcId)).ToList().ForEach((x) =>
                            {
                                _iRpcOptions.Remove(x);
                            });
                        }

                        if (_iRpcOptions.Count > listOfId.Count)
                        {
                            int diff = _iRpcOptions.Count - listOfId.Count;
                            for (int i = 0; i < diff; i++)
                                _iRpcOptions.RemoveAt(_iRpcOptions.Count - 1);
                        }
                        _iRpcOptions = _iRpcOptions.OrderBy(x => x.RpcId).ToList();
                    }
                    else
                    {
                        if (_iRpcOptions != null)
                            _iRpcOptions.Clear();
                    }
                }
            }
            #endregion
#endif
        }
        #endregion

        #region RPC
        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.<br/>
        ///* Prepara uma chamada iRPC na rede.
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        protected NeutronStream.IWriter Begin_iRPC(byte id, NeutronStream parameters, out iRpcOptions options)
        {
            if (RuntimeIRpcOptions.TryGetValue(id, out options))
                return This.Begin_iRPC(parameters);
            return null;
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        protected void End_iRPC(byte id, NeutronStream parameters) => End_iRPC(id, parameters, NeutronView);

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="view">* O Objeto de rede de destino.</param>
        protected void End_iRPC(byte id, NeutronStream parameters, NeutronView view)
        {
            if (RuntimeIRpcOptions.TryGetValue(id, out iRpcOptions option)) //* como é a perfomance disso?
                This.End_iRPC(parameters, view, option.RpcId, option.OriginalInstance.Id, option.CacheMode, option.TargetTo, option.Protocol, IsServer);
            else
                LogHelper.Error($"Rpc [{id}] not found!");
        }

        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados via rede.<br/>
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
        protected void End_gRPC(byte id, NeutronStream parameters, Protocol protocol)
        {
            if (!IsServer)
                End_gRPC(id, parameters, protocol, This);
            else
                End_gRPC(id, parameters, protocol, Owner);
        }
        #endregion

        #region Virtual Methods
        /// <summary>
        ///* Define o stream a ser usado para serializar os dados, somente se "HighPerformance" for verdadeiro.<br/>
        ///* Utilizado para serialização de dados de alta performance, reduz alocações no GC.<br/>
        /// </summary>
        /// <returns></returns>
        protected virtual NeutronStream GetPacketStream() => _packetStream;

        /// <summary>
        ///* Usado para personalizar a sincronização de variáveis ​​em um script monitorado por um NeutronView.<br/>
        ///* É determinado automaticamente se as variáveis ​​que estão sendo serializadas devem ser enviadas ou recebidas.<br/>
        ///* Usa o nível de autoridade definido no Inspetor.<br/>
        ///* O stream é descartado automaticamente, não é necessário o descarte manual.
        /// </summary>
        /// <param name="stream">* Fluxo usado para escrever ou ler os parâmetros enviados ou recebidos.</param>
        /// <param name="isMine">* Define se você está escrevendo ou lendo os dados.</param>
        public virtual bool OnAutoSynchronization(NeutronStream stream, bool isMine) => OnValidateAutoSynchronization(isMine);

        /// <summary>
        ///* Usado para validar "OnAutoSynchronization" ao lado do cliente ou servidor.
        /// </summary>
        /// <param name="isMine">Se "True", Validação ocorre ao lado do Cliente, se "False", ocorre ao lado do Servidor.</param>
        /// <returns></returns>
        protected virtual bool OnValidateAutoSynchronization(bool isMine) => true;

        /// <summary>
        ///* Implemente um nível personalizado de autoridade.
        /// </summary>
        protected virtual bool OnCustomAuthority() => throw new Exception("Custom Authority not implemented!");
        #endregion
    }
}