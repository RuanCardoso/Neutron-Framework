using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Client;
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
                        if (localInstance.isLocalPlayer(mPlayer)) localInstance.NeutronView = neutronView;
                        localInstance.networkObjects.TryAdd(mPlayer.ID, neutronView);
                    }
                    else if (isServer)
                    {
                        Player player = Neutron.Server.GetPlayer(mPlayer.tcpClient);
                        if (player != null)
                        {
                            player.NeutronView = neutronView;
                            Utils.ChangeColor(neutronView);
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

        private static void RegisterSceneObject(Player mPlayer, GameObject objectToRegister, bool isServer, Neutron localInstance)
        {
            if (isServer) objectToRegister = MonoBehaviour.Instantiate(objectToRegister);
            if (objectToRegister.TryGetComponent<NeutronView>(out NeutronView neutronView))
            {
                if (neutronView.ID > 0)
                {
                    neutronView.owner = mPlayer;
                    neutronView.isServer = isServer;
                    if (neutronView.enabled)
                        neutronView.OnNeutronAwake();
                    if (!isServer)
                    {
                        neutronView._ = localInstance;
                        localInstance.networkObjects.TryAdd(neutronView.ID, neutronView);
                    }
                    else if (isServer)
                    {
                        Utils.MoveToContainer(objectToRegister, "[Container] -> Server");
                        Neutron.Server.networkObjects.TryAdd(neutronView.ID, neutronView);
                        Utils.ChangeColor(neutronView);
                    }
                    LoadNeutronBehaviours(neutronView);
                }
                else if (!NeutronUtils.LoggerError("Scene objects must have their ID at > 0."))
                    MonoBehaviour.Destroy(objectToRegister);
            }
            else if (!NeutronUtils.LoggerError("\"Neutron View\" object not found, failed to register object in network."))
                MonoBehaviour.Destroy(objectToRegister);
        }

        public static void RegisterSceneObject(Player mPlayer, bool isServer, Neutron localInstance)
        {
            NeutronView[] neutronViews = GameObject.FindObjectsOfType<NeutronView>().Where(x => x.ID > 0).ToArray();
            foreach (NeutronView neutronView in neutronViews)
            {
                NeutronRegister.RegisterSceneObject(mPlayer, neutronView.gameObject, isServer, localInstance);
            }
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