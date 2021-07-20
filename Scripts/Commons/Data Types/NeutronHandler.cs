using System;
using NeutronNetwork.Constants;
using UnityEngine;

namespace NeutronNetwork.Server.Internal
{
    [Serializable]
    public class NeutronDefaultHandlerOptions
    {
        #region Fields
        [SerializeField] private TargetTo _targetTo;
        [SerializeField] private TunnelingTo _tunnelingTo;
        /*[SerializeField] */private Cache _cache;
        [SerializeField] private Protocol _protocol;
        #endregion

        #region Properties
        public TargetTo TargetTo { get => _targetTo; set => _targetTo = value; }
        public TunnelingTo TunnelingTo { get => _tunnelingTo; set => _tunnelingTo = value; }
        public Cache Cache { get => _cache; set => _cache = value; }
        public Protocol Protocol { get => _protocol; set => _protocol = value; }
        #endregion

        public NeutronDefaultHandlerOptions(TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            _targetTo = targetTo;
            _tunnelingTo = tunnelingTo;
            _protocol = protocol;
        }
    }

    [Serializable]
    public class NeutronDataSyncOptions : IEquatable<NeutronDataSyncOptions>
    {
        #region
        [SerializeField] private int _rpcId;
        [SerializeField] private int _instanceId;
        [SerializeField] private TargetTo _tergetTo;
        [SerializeField] private TunnelingTo _tunnelingTo;
        [SerializeField] private Cache _cache;
        [SerializeField] private Protocol _recProtocol;
        [SerializeField] private Protocol _sendProtocol;
        #endregion

        #region Properties
        public int RpcId { get => _rpcId; set => _rpcId = value; }
        public int InstanceId { get => _instanceId; set => _instanceId = value; }
        public TargetTo TergetTo { get => _tergetTo; set => _tergetTo = value; }
        public TunnelingTo TunnelingTo { get => _tunnelingTo; set => _tunnelingTo = value; }
        public Cache Cache { get => _cache; set => _cache = value; }
        public Protocol RecProtocol { get => _recProtocol; set => _recProtocol = value; }
        public Protocol SendProtocol { get => _sendProtocol; set => _sendProtocol = value; }
        #endregion

        public bool Equals(NeutronDataSyncOptions other)
        {
            return RpcId == other.RpcId && InstanceId == other.InstanceId;
        }
    }

    [Serializable]
    public class NeutronDataSyncOptionsWithRate
    {
        [SerializeField] private TargetTo _targetTo;
        [SerializeField] private TunnelingTo _tunnelingTo;
        [SerializeField] private Cache _cache;
        [SerializeField] private Protocol _recProtocol;
        [SerializeField] private Protocol _sendProtocol;
        [SerializeField] [Range(NeutronConstants.MIN_SEND_RATE, NeutronConstants.MAX_SEND_RATE)] private int _sendRate = 15; //* Quantidade de sincronizações por segundo.

        public TargetTo TargetTo { get => _targetTo; set => _targetTo = value; }
        public TunnelingTo TunnelingTo { get => _tunnelingTo; set => _tunnelingTo = value; }
        public Cache Cache { get => _cache; set => _cache = value; }
        public Protocol RecProtocol { get => _recProtocol; set => _recProtocol = value; }
        public Protocol SendProtocol { get => _sendProtocol; set => _sendProtocol = value; }
        public int SendRate { get => _sendRate; set => _sendRate = value; }
    }
}