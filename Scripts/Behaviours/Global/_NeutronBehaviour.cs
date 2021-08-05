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
        [SerializeField] [ReadOnly] private byte _id;
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
        public byte ID => _id;
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
        ///* Definido quando o servidor tem a autoridade sobre o objeto, isto é, impede que o servidor execute a sí mesmo alguma instrução que faz parte do iRPC ou OnAutoSynchronization.<br/>
        ///* Se o Cliente possuir a autoridade sobre o objeto, retorna "True".
        /// </summary>
        protected bool DoNotPerformTheOperationOnTheServer => IsClient || Authority != Authoritys.Server;
        /// <summary>
        ///* Retorna o seu objeto de rede.
        /// </summary>
        /// <value></value>
        public NeutronView NeutronView { get; set; }
        /// <summary>
        ///* A instância de Neutron a qual este objeto pertence.
        /// </summary>
        public Neutron This { get; set; }
        #endregion

        #region Custom MonoBehaviour Methods
        /// <summary>
        ///* É Seguro para chamadas internas.(IsMine, HasAuthority, IsServer).
        /// </summary>
        public virtual void OnNeutronStart()
        {
            This = NeutronView.This;
            foreach (iRpcOptions option in _iRpcOptions)
            {
                if (option.Instance.ID == ID)
                    RuntimeIRpcOptions.Add(option.RpcId, option);
                else
                    NeutronView.NeutronBehaviours[option.Instance.ID].RuntimeIRpcOptions.Add(option.RpcId, option);
            }
            //* Define que está pronto para uso, antes disso, tudo falhará.
            _isInitialized = true;
            //* Inicia a Auto Sincronização.
            StartCoroutine(InitializeAutoSync());
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
        [SerializeField] [ShowIf("_hasIRPC")] [Label("iRpcOptions")] private List<iRpcOptions> _iRpcOptions = new List<iRpcOptions>();
        [SerializeField] [HorizontalLineDown] [ShowIf("_hasOnAutoSynchronization")] private AutoSyncOptions OnAutoSynchronizationOptions;
        [NonSerialized] public Dictionary<byte, iRpcOptions> RuntimeIRpcOptions = new Dictionary<byte, iRpcOptions>();
        #endregion

        #region MonoBehaviour
        public virtual void Awake()
        { }

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

        private void LoadOptions()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var neutronBehaviours = transform.root.GetComponentsInChildren<NeutronBehaviour>();
                if (neutronBehaviours.Length <= byte.MaxValue)
                {
                    if (ID == 0)
                    {
                        _id = (byte)UnityEngine.Random.Range(1, byte.MaxValue);
                        if (neutronBehaviours.Count(x => x._id == _id) > 1)
                            Reset();
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

        #region Neutron
        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
#pragma warning disable IDE1006
        protected void iRPC(byte id, NeutronWriter writer)
#pragma warning restore IDE1006
        {
            if (RuntimeIRpcOptions.TryGetValue(id, out iRpcOptions option))
                NeutronView.This.iRPC(writer, NeutronView, option.RpcId, option.OriginalInstance.ID, option.Cache, option.TargetTo, option.Protocol, IsServer);
            else
                LogHelper.Error($"Rpc [{id}] not found!");
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="neutronView">* O Objeto de rede de destino.</param>
#pragma warning disable IDE1006
        protected void iRPC(byte id, NeutronWriter writer, NeutronView neutronView)
#pragma warning restore IDE1006
        {
            if (RuntimeIRpcOptions.TryGetValue(id, out iRpcOptions option))
                NeutronView.This.iRPC(writer, neutronView, option.RpcId, option.OriginalInstance.ID, option.Cache, option.TargetTo, option.Protocol, IsServer);
            else
                LogHelper.Error($"Rpc [{id}] not found!");
        }
        #endregion

        #region Virtual Methods and Enumerators
        private IEnumerator InitializeAutoSync()
        {
            while (_hasOnAutoSynchronization && HasAuthority)
            {
                var option = OnAutoSynchronizationOptions;
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    if (OnAutoSynchronization(writer, null, true)) //* Invoca o metódo.
                        NeutronView.This.OnAutoSynchronization(writer, NeutronView, ID, option.Protocol, IsServer); //* Envia para a rede.
                }
                yield return new WaitForSeconds(NeutronConstantsSettings.ONE_PER_SECOND / option.SendRate); //* SendRate, envios por segundo.
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