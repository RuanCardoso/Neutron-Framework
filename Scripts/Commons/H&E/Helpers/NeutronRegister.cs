using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using System;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    public class NeutronRegister
    {
        public static void RegisterPlayer(NeutronPlayer player, NeutronView view, bool isServer, Neutron instance)
        {
            string clientContainerType = isServer ? "Server" : "Client";
            if (view != null)
            {
                if (view.ID == 0)
                {
                    view.Owner = player;
                    view.IsServer = isServer;
                    view.name = player.Nickname + $" -> [{clientContainerType}]";
                    view.ID = player.ID;
                    if (view.enabled)
                        view.OnNeutronAwake();
                    if (!isServer)
                    {
                        view._ = instance;
                        if (instance.IsMine(player)) instance.NeutronView = view;
                        instance.NetworkObjects.TryAdd(player.ID, view);
                    }
                    else if (isServer)
                    {
                        if (SocketHelper.GetPlayer(player.m_TcpClient, out NeutronPlayer nPlayer))
                        {
                            nPlayer.NeutronView = view;
                            ChangeColor(view);
                        }
                        else LogHelper.Error("Neutron View Object has been destroyed?");
                    }
                    LoadNeutronBehaviours(view);
                }
                else if (!LogHelper.Error("Dynamically instantiated objects must have their ID at 0."))
                    MonoBehaviour.Destroy(view);
            }
            else if (!LogHelper.Error("\"Neutron View\" object not found, failed to instantiate in network."))
                MonoBehaviour.Destroy(view);
        }

        public static void RegisterObject(NeutronPlayer player, NeutronView view, int id, bool isServer, Neutron instance)
        {
            if (view.ID == 0)
            {
                view.Owner = player;
                view.IsServer = isServer;
                view.ID = id;
                if (view.enabled)
                    view.OnNeutronAwake();
                if (!isServer && instance.NetworkObjects.TryAdd(view.ID, view))
                    view._ = instance;
                else if (isServer)
                {
                    ChangeColor(view);
                    if (player.IsInRoom())
                    {
                        Neutron.Server.ChannelsById[player.CurrentChannel]
                             .GetRoom(player.CurrentRoom).SceneSettings.networkObjects
                             .Add(view.ID, view);
                    }
                    else if (player.IsInChannel())
                    {
                        Neutron.Server.ChannelsById[player.CurrentChannel].SceneSettings.networkObjects
                            .Add(view.ID, view);
                    }
                    else LogHelper.Error("Network scene objects, require a channel or room.");
                }
                LoadNeutronBehaviours(view);
            }
            else if (!LogHelper.Error("Dynamically instantiated objects must have their ID at 0."))
                MonoBehaviour.Destroy(view);
        }

        public static void RegisterSceneObject(NeutronPlayer player, NeutronView view, bool isServer, Neutron instance = null)
        {
            if (view.ID > 0)
            {
                view.Owner = player;
                view.IsServer = isServer;
                if (view.enabled)
                    view.OnNeutronAwake();
                if (!isServer && instance.NetworkObjects.TryAdd(view.ID, view))
                    view._ = instance;
                else if (isServer)
                {
                    ChangeColor(view);
                    if (player.IsInRoom())
                    {
                        Neutron.Server.ChannelsById[player.CurrentChannel]
                             .GetRoom(player.CurrentRoom).SceneSettings.networkObjects
                             .Add(view.ID, view);
                    }
                    else if (player.IsInChannel())
                    {
                        Neutron.Server.ChannelsById[player.CurrentChannel].SceneSettings.networkObjects
                            .Add(view.ID, view);
                    }
                    else LogHelper.Error("Network scene objects, require a channel or room.");
                }
                LoadNeutronBehaviours(view);
            }
            else if (!LogHelper.Error("Scene objects must have their ID at > 0."))
                MonoBehaviour.Destroy(view.gameObject);
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