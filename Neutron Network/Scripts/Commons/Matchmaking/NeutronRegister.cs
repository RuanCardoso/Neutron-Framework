using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Server;
using System;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    public class NeutronRegister
    {
        public static void RegisterPlayer(Player mPlayer, NeutronView neutronView, bool isServer, Neutron localInstance)
        {
            string clientContainerType = isServer ? "Server" : "Client";
            if (neutronView != null)
            {
                if (neutronView.ID == 0)
                {
                    neutronView.Owner = mPlayer;
                    neutronView.IsServer = isServer;
                    neutronView.name = mPlayer.Nickname + $" -> [{clientContainerType}]";
                    neutronView.ID = mPlayer.ID;
                    if (neutronView.enabled)
                        neutronView.OnNeutronAwake();
                    if (!isServer)
                    {
                        neutronView._ = localInstance;
                        if (localInstance.IsMine(mPlayer)) localInstance.NeutronView = neutronView;
                        localInstance.NetworkObjects.TryAdd(mPlayer.ID, neutronView);
                    }
                    else if (isServer)
                    {
                        if (SocketHelper.GetPlayer(mPlayer.tcpClient, out Player nPlayer))
                        {
                            nPlayer.NeutronView = neutronView;
                            ChangeColor(neutronView);
                        }
                        else NeutronLogger.LoggerError("Neutron View Object has been destroyed?");
                    }
                    LoadNeutronBehaviours(neutronView);
                }
                else if (!NeutronLogger.LoggerError("Dynamically instantiated objects must have their ID at 0."))
                    MonoBehaviour.Destroy(neutronView);
            }
            else if (!NeutronLogger.LoggerError("\"Neutron View\" object not found, failed to instantiate in network."))
                MonoBehaviour.Destroy(neutronView);
        }

        public static void RegisterObject(Player mPlayer, NeutronView neutronView, int uniqueID, bool isServer, Neutron localInstance)
        {
            if (neutronView.ID == 0)
            {
                neutronView.Owner = mPlayer;
                neutronView.IsServer = isServer;
                neutronView.ID = uniqueID;
                if (neutronView.enabled)
                    neutronView.OnNeutronAwake();
                if (!isServer && localInstance.NetworkObjects.TryAdd(neutronView.ID, neutronView))
                    neutronView._ = localInstance;
                else if (isServer)
                {
                    ChangeColor(neutronView);
                    if (mPlayer.IsInRoom())
                    {
                        Neutron.Server.ChannelsById[mPlayer.CurrentChannel]
                             .GetRoom(mPlayer.CurrentRoom).SceneSettings.networkObjects
                             .Add(neutronView.ID, neutronView);
                    }
                    else if (mPlayer.IsInChannel())
                    {
                        Neutron.Server.ChannelsById[mPlayer.CurrentChannel].SceneSettings.networkObjects
                            .Add(neutronView.ID, neutronView);
                    }
                    else NeutronLogger.LoggerError("Network scene objects, require a channel or room.");
                }
                LoadNeutronBehaviours(neutronView);
            }
            else if (!NeutronLogger.LoggerError("Dynamically instantiated objects must have their ID at 0."))
                MonoBehaviour.Destroy(neutronView);
        }

        public static void RegisterSceneObject(Player mPlayer, NeutronView neutronView, bool isServer, Neutron localInstance = null)
        {
            if (neutronView.ID > 0)
            {
                neutronView.Owner = mPlayer;
                neutronView.IsServer = isServer;
                if (neutronView.enabled)
                    neutronView.OnNeutronAwake();
                if (!isServer && localInstance.NetworkObjects.TryAdd(neutronView.ID, neutronView))
                    neutronView._ = localInstance;
                else if (isServer)
                {
                    ChangeColor(neutronView);
                    if (mPlayer.IsInRoom())
                    {
                        Neutron.Server.ChannelsById[mPlayer.CurrentChannel]
                             .GetRoom(mPlayer.CurrentRoom).SceneSettings.networkObjects
                             .Add(neutronView.ID, neutronView);
                    }
                    else if (mPlayer.IsInChannel())
                    {
                        Neutron.Server.ChannelsById[mPlayer.CurrentChannel].SceneSettings.networkObjects
                            .Add(neutronView.ID, neutronView);
                    }
                    else NeutronLogger.LoggerError("Network scene objects, require a channel or room.");
                }
                LoadNeutronBehaviours(neutronView);
            }
            else if (!NeutronLogger.LoggerError("Scene objects must have their ID at > 0."))
                MonoBehaviour.Destroy(neutronView.gameObject);
        }

        private static void LoadNeutronBehaviours(NeutronView neutronView)
        {
            var neutronBehaviours = neutronView.GetComponentsInChildren<NeutronBehaviour>();
            foreach (var neutronBehaviour in neutronBehaviours)
            {
                neutronBehaviour.NeutronView = neutronView;
                if (neutronBehaviour.enabled)
                    neutronBehaviour.OnNeutronStart();
            }
            neutronView.OnNeutronStart();
        }

        static void ChangeColor(NeutronView neutronView)
        {
            Renderer renderer = neutronView.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.red;
        }
    }
}