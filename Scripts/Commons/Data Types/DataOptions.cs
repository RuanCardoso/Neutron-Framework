using NeutronNetwork.Constants;
using NeutronNetwork.Naughty.Attributes;
using System;
using UnityEngine;

namespace NeutronNetwork.Server.Internal
{
    [Serializable]
    public class HandlerOptions
    {
        #region Fields
        [SerializeField] private TargetTo _targetTo;
        [SerializeField] private TunnelingTo _tunnelingTo;
        [SerializeField] private Protocol _protocol;
        #endregion

        #region Properties
        public TargetTo TargetTo { get => _targetTo; set => _targetTo = value; }
        public TunnelingTo TunnelingTo { get => _tunnelingTo; set => _tunnelingTo = value; }
        public Protocol Protocol { get => _protocol; set => _protocol = value; }
        #endregion

        public HandlerOptions(TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            _targetTo = targetTo;
            _tunnelingTo = tunnelingTo;
            _protocol = protocol;
        }
    }

    [Serializable]
#pragma warning disable IDE1006
    public class iRpcOptions : ISerializationCallbackReceiver, IEquatable<iRpcOptions>
#pragma warning restore IDE1006
    {
#pragma warning disable IDE0052
        [SerializeField] [HideInInspector] private string Title = "Neutron";
#pragma warning restore IDE0052

        #region
        [SerializeField] [AllowNesting] [ReadOnly] private byte _rpcId;
        [SerializeField] [AllowNesting] [ReadOnly] private string _name;
        [SerializeField] private NeutronBehaviour _instance;
        [SerializeField] [HideInInspector] private NeutronBehaviour _originalInstance;
        [SerializeField] private TargetTo _targetTo;
        [SerializeField] private Cache _cache;
        [SerializeField] private Protocol _protocol;
        #endregion

        #region Properties
        public byte RpcId { get => _rpcId; set => _rpcId = value; }
        public string Name { get => _name; set => _name = value; }
        public NeutronBehaviour Instance { get => _instance; set => _instance = value; }
        public NeutronBehaviour OriginalInstance { get => _originalInstance; set => _originalInstance = value; }
        public TargetTo TargetTo { get => _targetTo; set => _targetTo = value; }
        public Cache Cache { get => _cache; set => _cache = value; }
        public Protocol Protocol { get => _protocol; set => _protocol = value; }

        public Boolean Equals(iRpcOptions other)
        {
            return other.RpcId == RpcId && other.OriginalInstance.ID == OriginalInstance.ID;
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            Title = _name;
#endif
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            Title = _name;
#endif
        }
        #endregion
    }

    [Serializable]
    public class AutoSyncOptions
    {
        [SerializeField] private Protocol _protocol;
        [SerializeField] [Range(NeutronConstantsSettings.MIN_SEND_RATE, NeutronConstantsSettings.MAX_SEND_RATE)] private int _sendRate = 15; //* Quantidade de sincronizações por segundo.

        public Protocol Protocol { get => _protocol; set => _protocol = value; }
        public int SendRate { get => _sendRate; set => _sendRate = value; }
    }
}