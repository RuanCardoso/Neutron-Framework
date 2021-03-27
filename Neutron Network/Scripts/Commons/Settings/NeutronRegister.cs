using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server;
using System;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronRegister
    {
        public static GameObject RegisterPlayer(Player mPlayer, GameObject objectInst, bool isServer, Neutron localInstance)
        {
            string clientContainerType = isServer ? "Server" : "Client";
            if (objectInst.TryGetComponent<NeutronView>(out NeutronView neutronView))
            {
                if (neutronView.ID == 0)
                {
                    neutronView.owner = mPlayer;
                    neutronView.isServer = isServer;
                    neutronView.name = (!mPlayer.IsBot) ? mPlayer.Nickname + $" -> [{clientContainerType}]" : mPlayer.Nickname + " -> [Bot]";
                    neutronView.ID = mPlayer.ID;
                    if (neutronView.enabled)
                        neutronView.OnNeutronAwake();
                    if (!isServer)
                    {
                        neutronView._ = localInstance;
                        if (localInstance.IsMine(mPlayer)) localInstance.NeutronView = neutronView;
                        localInstance.networkObjects.TryAdd(mPlayer.ID, neutronView);
                    }
                    else if (isServer)
                    {
                        if (Neutron.Server.GetPlayer(mPlayer.tcpClient, out Player nPlayer))
                        {
                            nPlayer.NeutronView = neutronView;
                            InternalUtils.ChangeColor(neutronView);
                        }
                        else NeutronUtils.LoggerError("Neutron View Object has been destroyed?");
                    }
                    LoadNeutronBehaviours(neutronView);
                }
                else if (!NeutronUtils.LoggerError("Dynamically instantiated objects must have their ID at 0."))
                    MonoBehaviour.Destroy(objectInst);
            }
            else if (!NeutronUtils.LoggerError("\"Neutron View\" object not found, failed to instantiate in network."))
                MonoBehaviour.Destroy(objectInst);
            return objectInst;
        }

        public static void RegisterSceneObject(Player mPlayer, NeutronView neutronView, bool isServer, Neutron localInstance = null)
        {
            if (neutronView.ID > 0)
            {
                neutronView.owner = mPlayer;
                neutronView.isServer = isServer;
                if (neutronView.enabled)
                    neutronView.OnNeutronAwake();
                if (!isServer && localInstance.networkObjects.TryAdd(neutronView.ID, neutronView))
                    neutronView._ = localInstance;
                else if (isServer)
                {
                    InternalUtils.ChangeColor(neutronView);
                    if (mPlayer.IsInRoom())
                    {
                        Neutron.Server.ChannelsById[mPlayer.CurrentChannel]
                             .GetRoom(mPlayer.CurrentRoom).sceneSettings.networkObjects
                             .Add(neutronView.ID, neutronView);
                    }
                    else if (mPlayer.IsInChannel())
                    {
                        Neutron.Server.ChannelsById[mPlayer.CurrentChannel].sceneSettings.networkObjects
                            .Add(neutronView.ID, neutronView);
                    }
                    else NeutronUtils.LoggerError("Network scene objects, require a channel or room.");
                }
                LoadNeutronBehaviours(neutronView);
            }
            else if (!NeutronUtils.LoggerError("Scene objects must have their ID at > 0."))
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
    }
}