using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Server;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronRegister
    {
        public static GameObject RegisterPlayer(Neutron localInstance, GameObject prefabPlayer, Player mPlayer, bool isServer)
        {
            try
            {
                NeutronView view = prefabPlayer.AddComponent<NeutronView>();
                view.isServerOrClient = isServer;
                view.owner = mPlayer;

                if (!isServer)
                {

                    view.name = (!mPlayer.IsBot) ? mPlayer.Nick + " -> [Client]" : mPlayer.Nick + " -> [Bot]";
                    view._ = localInstance;
                    if (localInstance.isLocalPlayer(mPlayer)) localInstance.NeutronView = view;
                    localInstance.playersObjects.TryAdd(mPlayer.ID, view);
                    //localInstance.onPlayerInstantiated?.Invoke(mPlayer, prefabPlayer, localInstance);
                }
                else if (isServer)
                {

                    Neutron.Server.Players[mPlayer.tcpClient].NeutronView = view;

                    Renderer renderer = view.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                        renderer.material.color = Color.red;

                    prefabPlayer.name = (!mPlayer.IsBot) ? mPlayer.Nick + " -> [Server]" : mPlayer.Nick + " -> [Bot]";
                    //NeutronSFunc.onPlayerInstantiated?.Invoke(mPlayer);
                }

                var neutronBehaviours = view.GetComponentsInChildren<NeutronBehaviour>();
                foreach (var neutronBehaviour in neutronBehaviours)
                {
                    neutronBehaviour.NeutronView = view;
                    neutronBehaviour.OnNeutronStart();
                }
            }
            catch (Exception ex) { Utilities.StackTrace(ex); }
            return prefabPlayer;
        }
    }
}