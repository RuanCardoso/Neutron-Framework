using NeutronNetwork.Attributes;
using NeutronNetwork.Components;
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

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork
{
    /// <summary>
    ///* This class is the base class for all objects that can be controlled by the network.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_BEHAVIOUR)]
    public class NeutronBehaviour : GlobalBehaviour
    {
        /// <summary>
        ///* Stream used by the network to send and receive data.
        /// </summary>
        /// <returns></returns>
        private readonly NeutronStream _packetStream = new NeutronStream();

        #region Fields -> Inspector
        /// <summary>
        ///* The network ID of the object.
        /// </summary>
        /// <returns></returns>
        [Header("[Identity]")]
        [SerializeField] private byte _id;
        /// <summary>
        ///* Define if the authority is handled by other object.
        /// </summary>
        /// <returns></returns>
#pragma warning disable IDE0044
        [SerializeField] [ShowIf("_authority", AuthorityMode.Handled)] private NeutronBehaviour _authorityHandledBy;
#pragma warning restore IDE0044
        /// <summary>
        ///* All scripts work as if the object registered in the network.
        /// </summary>
        [SerializeField] protected bool _offlineMode = false;
        /// <summary>
        ///* The level of authority of the object.
        /// </summary>
        [SerializeField] [HorizontalLineDown] [InfoBox("\"HasAuthority\" returns this property.")] [HideIf("_offlineMode")] private AuthorityMode _authority = AuthorityMode.Mine;
        [HideInInspector]
        [SerializeField] private bool _hasOnAutoSynchronization, _hasIRPC;
        #endregion

        #region Fields
        /// <summary>
        ///* Timer of auto synchronization.
        /// </summary>
        private float _autoSyncTimeDelay;
        /// <summary>
        ///* Store the auto synchronization options.
        /// </summary>
        /// <returns></returns>
#pragma warning disable IDE0044
        [SerializeField] [HorizontalLineDown] [ShowIf("_hasOnAutoSynchronization")] private AutoSyncOptions _onAutoSynchronizationOptions = new AutoSyncOptions();
#pragma warning restore IDE0044
        /// <summary>
        ///* Authority controller for the local object.
        /// </summary>
        [SerializeField] [HideInInspector] protected NeutronAuthority NeutronAuthority;
        #endregion

        #region Properties
        /// <summary>
        ///* The id used to identify the object on the network.
        /// </summary>
        public byte Id => _id;

        /// <summary>
        ///* Returns the level of authority of the object.
        /// </summary>
        protected AuthorityMode Authority => _authority;

        /// <summary>
        ///* Override to set the auto sync authority level.
        /// </summary>
        protected virtual bool AutoSyncAuthority => HasAuthority;

        /// <summary>
        ///* Returns if the object has registered on the network.
        /// </summary>
        /// <value></value>
        public bool IsRegistered
        {
            get;
            private set;
        }

        /// <summary>
        ///* Returns if you has authority over the object.
        /// </summary>
        /// <returns></returns>
        protected bool IsMine => IsRegistered && This.IsMine(Owner);

        /// <summary>
        ///* Returns if you is the owner of matchmaking.
        /// </summary>
        protected bool IsMasterClient => IsRegistered && Owner.IsMaster;

        /// <summary>
        ///* Returns if server has authority over the object.
        /// </summary>
        protected bool IsServer => IsRegistered && NeutronView.IsServer;

        /// <summary>
        ///* Returns is client has authority over the object.
        /// </summary>
        protected bool IsClient => IsRegistered && !IsServer;

        /// <summary>
        ///* Implements custom authority logic.
        /// </summary>
        /// <returns></returns>
        protected bool IsCustom => IsRegistered && OnCustomAuthority();

        /// <summary>
        ///* Returns if the authority is handled by other object.
        /// </summary>
        protected bool IsHandled => IsRegistered && (_authorityHandledBy != null && _authorityHandledBy.HasAuthority);

        /// <summary>
        ///* All players has authority over this bject.
        /// </summary>
        protected bool IsFree => IsRegistered;

        /// <summary>
        ///* Simplified version of <see cref="IsMine"/> or <see cref="IsCustom"/> or <see cref="IsServer"/> or <see cref="IsClient"/> or <see cref="IsMasterClient"/> or <see cref="IsHandled"/> and others...<br/>
        ///* Returns based on the authority mode defined in the inspector.
        /// </summary>
        /// <value></value>
        protected bool HasAuthority
        {
            get
            {
                switch (Authority)
                {
                    case AuthorityMode.Mine:
                        return IsMine; //* If the authority is mine, return true.
                    case AuthorityMode.Client:
                        return IsClient; //* If the authority is server, return true.
                    case AuthorityMode.Server:
                        return IsServer; //* If the authority is server, return true.
                    case AuthorityMode.Master:
                        return IsMasterClient; //* If the authority is master client, return true.
                    case AuthorityMode.All:
                        return IsFree; //* If the authority is all, return true.
                    case AuthorityMode.Custom:
                        return IsCustom; //* If the authority is custom, return the result of <see cref="OnCustomAuthority"/>.
                    case AuthorityMode.Handled:
                        return IsHandled; //* If the authority is handled, return the result of <see cref="_authorityHandledBy"/>.
                    case AuthorityMode.None:
                        return true;
                    default:
                        return LogHelper.Error("Authority not implemented!");
                }
            }
        }

        /// <summary>
        /// * Set when the server has authority over the object, that is, to prevent the server from executing itself an instruction that is part of the iRPC or OnAutoSynchronization. <br/>
        /// * If the Client has an authority on the object, returns "true".
        /// </summary>
        protected bool DoNotPerformTheOperationOnTheServer => IsClient || Authority != AuthorityMode.Server;

        /// <summary>
        ///* Returns the identity of the object.
        /// </summary>
        /// <value></value>
        public NeutronView NeutronView
        {
            get;
            set;
        }

        /// <summary>
        ///* Returns the instance who owns the object.
        /// </summary>
        protected Neutron This => NeutronView.This;

        /// <summary>
        ///* Returns the owner of the object.
        /// </summary>
        protected NeutronPlayer Owner => NeutronView.Owner;

        /// <summary>
        ///* Returns the scene who owns the object.
        /// </summary>
        protected Scene Scene => gameObject.scene;

        /// <summary>
        ///* Returns the 3D physics scene used by the object.
        /// </summary>
        /// <returns></returns>
        protected PhysicsScene Physics3D => Scene.GetPhysicsScene();

        /// <summary>
        ///* Returns the 2D physics scene used by the object.
        /// </summary>
        /// <returns></returns>
        protected PhysicsScene2D Physics2D => Scene.GetPhysicsScene2D();

        /// <summary>
        ///* Returns the network time in seconds.
        /// </summary>
        protected double NetworkTime => This.NetworkTime.Time;

        /// <summary>
        ///* Returns the local time in seconds.
        /// </summary>
        protected double LocalTime => This.NetworkTime.LocalTime;

        /// <summary>
        ///* Returns the auto-sync options.
        /// </summary>
        public AutoSyncOptions AutoSyncOptions => _onAutoSynchronizationOptions;
        #endregion

        #region Collections
        /// <summary>
        ///* Store the all methods marked with iRPC attribute in the Editor.
        /// </summary>
        [SerializeField] [ShowIf("_hasIRPC")] [Label("iRpcOptions")] protected List<iRpcOptions> _iRpcOptions = new List<iRpcOptions>();
        /// <summary>
        ///* Store the all methods marked with iRPC attribute in the Runtime.
        /// </summary>
        [NonSerialized] protected readonly Dictionary<byte, iRpcOptions> RuntimeIRpcOptions = new Dictionary<byte, iRpcOptions>();
        #endregion

        #region Custom Mono Behaviour Methods
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

            foreach (iRpcOptions option in _iRpcOptions)
            {
                //* Add the iRPC options to the runtime dictionary.
                if (option.Instance.Id == Id)
                    RuntimeIRpcOptions.Add(option.RpcId, option); //* If the instance id is the same as the object id, add the option to the runtime dictionary.
                else
                    NeutronView.NeutronBehaviours[option.Instance.Id].RuntimeIRpcOptions.Add(option.RpcId, option); //* If the instance id is different, add the option to the runtime dictionary of the instance.
            }
            IsRegistered = true; //* Set the object as registered.
        }

        protected virtual void OnNeutronUpdate()
        {
            if (_hasOnAutoSynchronization)
            {
                _autoSyncTimeDelay -= Time.deltaTime; //* Decrease the auto-sync delay.
                if (_autoSyncTimeDelay <= 0)
                {
                    NeutronStream packetStream = GetPacketStream();
                    if (_hasOnAutoSynchronization && AutoSyncAuthority)
                    {
                        if (!_onAutoSynchronizationOptions.FixedSize)
                        {
                            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                            {
                                stream.Writer.SetPosition(PacketSize.AutoSync); //* Set the position of the stream to the auto-sync packet size.
                                if (OnAutoSynchronization(stream, true))
                                    This.OnAutoSynchronization(stream, NeutronView, Id, _onAutoSynchronizationOptions.Protocol, IsServer); //* Send the auto-sync packet.
                            }
                        }
                        else
                        {
                            //* If the stream has a fixed size, send the auto-sync packet with the fixed size.
                            packetStream.Writer.SetPosition(PacketSize.AutoSync); //* Set the position of the stream to the auto-sync packet size.
                            if (OnAutoSynchronization(packetStream, true))
                                This.OnAutoSynchronization(packetStream, NeutronView, Id, _onAutoSynchronizationOptions.Protocol, IsServer); //* Send the auto-sync packet.
                        }
                    }
                    _autoSyncTimeDelay = NeutronConstantsSettings.ONE_PER_SECOND / _onAutoSynchronizationOptions.PacketsPerSecond; //* Set the auto-sync delay per second.
                }
            }
        }

        protected virtual void OnNeutronFixedUpdate() { }
        protected virtual void OnNeutronLateUpdate() { }
        #endregion

        #region Mono Behaviour
        protected virtual void Update()
        {
            if (IsRegistered || _offlineMode)
                OnNeutronUpdate();
        }

        protected virtual void FixedUpdate()
        {
            if (IsRegistered || _offlineMode)
                OnNeutronFixedUpdate();
        }

        protected virtual void LateUpdate()
        {
            if (IsRegistered || _offlineMode)
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
            if (NeutronAuthority == null)
            {
                var neutronAuthorities = transform.root.GetComponentsInChildren<NeutronAuthority>().Where(x => x._root).ToArray();
                if (neutronAuthorities.Length > 0)
                {
                    var internAuthorities = transform.root.GetComponentsInChildren<NeutronAuthority>();
                    if (internAuthorities.Length > 1)
                        LogHelper.Error("Only one authority controller can exist when root mode is active.");
                    else
                    {
                        NeutronAuthority = neutronAuthorities[0];
                        if (NeutronAuthority != null && GetType().Name != "NeutronAuthority")
                            HandledBy(NeutronAuthority);
                    }
                }
                else
                {
                    NeutronAuthority = transform.GetComponent<NeutronAuthority>();
                    if (NeutronAuthority != null && GetType().Name != "NeutronAuthority")
                        HandledBy(NeutronAuthority);
                }
            }

            LoadOptions();
            if (_offlineMode)
                _authority = AuthorityMode.None;
#endif
        }

        public void HandledBy(NeutronBehaviour neutronBehaviour)
        {
            _authority = AuthorityMode.Handled;
            _authorityHandledBy = neutronBehaviour;
        }

        private void LoadOptions()
        {
#if UNITY_EDITOR //* If the application is in the editor.
            if (!Application.isPlaying)
            {
                var behaviours = transform.root.GetComponentsInChildren<NeutronBehaviour>(); //* Get all.......
                if (behaviours.Length <= byte.MaxValue)
                {
                    if (Id == 0)
                        _id = (byte)Helper.GetAvailableId(behaviours, x => x.Id, byte.MaxValue); //* If the object id is 0, get the available id.
                    else
                    {
                        if (!(Id >= byte.MaxValue))
                        {
                            //* If the object id is not 0 and less than the maximum value, check if the id is available.
                            int count = behaviours.Count(x => x.Id == Id); //* find duplicate id.
                            if (count > 1)
                                Reset(); //* If the id is already in use, reset the id.
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
                NeutronBehaviour instance = this; //* Get local instance.
                if (instance != null)
                {
                    var method = ReflectionHelper.GetMethod("OnAutoSynchronization", instance); //* Get the virtual auto-sync method.
                    if (method != null)
                        _hasOnAutoSynchronization = method.DeclaringType != typeof(NeutronBehaviour); //* If the method is not null, check if the method is not from the base class.
                    else
                        _hasOnAutoSynchronization = false;

                    (iRPCAttribute[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<iRPCAttribute>(instance); //* Get all the methods marked with iRPC attribute.
                    _hasIRPC = multiplesMethods.Length > 0; //* If the methods are not null, set the hasIRPC to true.
                    if (_hasIRPC && _iRpcOptions != null)
                    {
                        //* If the methods are not null, set the iRPC options.
                        List<byte> listOfId = new List<byte>(); //* Create a list of ids.
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            //* For each method marked with iRPC attribute, create a new iRPC option.
                            (iRPCAttribute[], MethodInfo) iRPCs = multiplesMethods[i];
                            for (int rI = 0; rI < iRPCs.Item1.Length; rI++)
                            {
                                //* For each iRPC attribute, create a new iRPC option.
                                iRPCAttribute iRPC = iRPCs.Item1[rI]; //* Get the iRPC attribute.

                                var option = new iRpcOptions
                                {
                                    Instance = instance,
                                    OriginalInstance = instance,
                                    RpcId = iRPC.Id,
                                    Name = iRPCs.Item2.Name,
                                }; //* Create a new iRPC option.

                                listOfId.Add(option.RpcId); //* Add the id to the list of ids.
                                if (!_iRpcOptions.Contains(option)) //* If the option is not in the list of iRPC options.
                                    _iRpcOptions.Add(option);//* Add the option to the list of iRPC options.
                                else
                                    continue; //* If the option is already in the list of iRPC options, continue.
                            }
                        }

                        if (_iRpcOptions.Count > listOfId.Count)
                        {
                            //* If the list of ids is less than the list of iRPC options, remove the extra options.
                            _iRpcOptions.Where(x => !listOfId.Contains(x.RpcId)).ToList().ForEach((x) =>
                            {
                                _iRpcOptions.Remove(x); //* Remove the iRPC options that are not in the list of ids.
                            });
                        }

                        if (_iRpcOptions.Count > listOfId.Count)
                        {
                            //* If the list of ids is less than the list of iRPC options, remove the extra options.
                            int diff = _iRpcOptions.Count - listOfId.Count;
                            for (int i = 0; i < diff; i++)
                                _iRpcOptions.RemoveAt(_iRpcOptions.Count - 1); //* Remove the extra options.
                        }
                        _iRpcOptions = _iRpcOptions.OrderBy(x => x.RpcId).ToList(); //* Order the iRPC options by id.
                    }
                    else
                    {
                        if (_iRpcOptions != null)
                            _iRpcOptions.Clear(); //* If the methods are null, clear the list of iRPC options.
                    }
                }
            }
            #endregion
#endif
        }
        #endregion

        #region Service Calls
        /// <summary>
        ///* Initiates a iRPC(Instance Remote Procedure Call) service call.<br/>
        /// </summary>
        /// <param name="id">The id of the iRPC service.</param>
        /// <param name="parameters">The parameters of the iRPC service.</param>
        /// <param name="options">Returns the current options of iRPC.</param>
        /// <returns></returns>
        protected NeutronStream.IWriter Begin_iRPC(byte id, NeutronStream parameters, out iRpcOptions options)
        {
            if (RuntimeIRpcOptions.TryGetValue(id, out options))
                return This.Begin_iRPC(parameters);
            return null;
        }

        /// <summary>
        ///* Ends a iRPC(Instance Remote Procedure Call) service call.<br/>
        /// </summary>
        /// <param name="id">The id of the iRPC service.</param>
        /// <param name="parameters">The parameters of the iRPC service.</param>
        protected void End_iRPC(byte id, NeutronStream parameters) => End_iRPC(id, parameters, NeutronView);

        /// <summary>
        ///* Ends a iRPC(Instance Remote Procedure Call) service call.<br/>
        /// </summary>
        /// <param name="id">The id of the iRPC service.</param>
        /// <param name="parameters">The parameters of the iRPC service.</param>
        /// <param name="view">The view to execute the iRPC service call on.</param>
        protected void End_iRPC(byte id, NeutronStream parameters, NeutronView view)
        {
            if (RuntimeIRpcOptions.TryGetValue(id, out iRpcOptions option)) //* If the option is found.
                This.End_iRPC(parameters, view, option.RpcId, option.OriginalInstance.Id, option.CacheMode, option.TargetTo, option.Protocol, IsServer); //* Execute the iRPC service call.
            else
                LogHelper.Error($"Rpc [{id}] not found!");
        }

        /// <summary>
        ///* Ends a gRPC(Global Remote Procedure Call) service call.<br/>
        /// </summary>
        /// <param name="id">The id of the gRPC service.</param>
        /// <param name="parameters">The parameters of the gRPC service.</param>
        /// <param name="protocol">The protocol to send the gRPC service call on</param>
        protected void End_gRPC(byte id, NeutronStream parameters, Protocol protocol)
        {
            if (!IsServer)
                End_gRPC(id, parameters, protocol, This); //* If the client, execute the gRPC service call.
            else
                End_gRPC(id, parameters, protocol, Owner); //* If the server, execute the gRPC service call.
        }
        #endregion

        #region Virtual Methods
        /// <summary>
        ///* Reduce the GC pressure, reusing the same instance of the <see cref="NeutronStream"/> with fixed size.
        /// </summary>
        /// <returns></returns>
        protected virtual NeutronStream GetPacketStream() => _packetStream;

        /// <summary>
        ///* Called by Neutron several times per second, so that your script can write and read synchronization data for the PhotonView.<br/>
        ///* Implementing this method, you can customize which data a NeutronView regularly synchronizes. Your code defines what is being sent (content) and how your data is used by receiving clients.
        /// </summary>
        /// <param name="stream">The stream to write or read data.</param>
        /// <param name="isMine">The IsMine property will be true if this client is the "owner" of the NeutronView (and thus the GameObject). Add data to the stream and it's sent via the server to the other players in a matchmaking. On the receiving side, IsMine is false and the data should be read.</param>
        /// <returns></returns>
        public virtual bool OnAutoSynchronization(NeutronStream stream, bool isMine) => OnValidateAutoSynchronization(isMine);

        /// <summary>
        ///* Used to validate the OnAutoSynchronization method, if returning false, the OnAutoSynchronization method will not be sended by the server, except if object not exists on the server.
        /// </summary>
        /// <param name="isMine">If true, the validate method will be called in client, otherwise will be called in server.</param>
        /// <returns></returns>
        protected virtual bool OnValidateAutoSynchronization(bool isMine) => true;

        /// <summary>
        ///* Implements your custom authority logic.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnCustomAuthority() => throw new Exception("Custom Authority not implemented!");
        #endregion
    }
}