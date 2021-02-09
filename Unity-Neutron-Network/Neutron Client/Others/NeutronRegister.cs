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

                var neutronBehaviours = view.GetComponentsInChildren<NeutronBehaviour>();
                foreach (var neutronBehaviour in neutronBehaviours) neutronBehaviour.NeutronView = view;

                if (!isServer)
                {

                    view.name = (!mPlayer.isBot) ? mPlayer.Nickname + " -> [Client]" : mPlayer.Nickname + " -> [Bot]";
                    view._ = localInstance;
                    if (localInstance.isLocalPlayer(mPlayer)) localInstance.NeutronView = view;
                    localInstance.playersObjects.TryAdd(mPlayer.ID, view);
                    //localInstance.onPlayerInstantiated?.Invoke(mPlayer, prefabPlayer, localInstance);
                }
                else if (isServer)
                {

                    Neutron.Server.Players[mPlayer.tcpClient].neutronView = view;

                    Renderer renderer = view.GetComponentInChildren<Renderer>();
                    if (renderer != null)
                        renderer.material.color = Color.red;

                    prefabPlayer.name = (!mPlayer.isBot) ? mPlayer.Nickname + " -> [Server]" : mPlayer.Nickname + " -> [Bot]";
                    NeutronSFunc.onPlayerInstantiated?.Invoke(mPlayer);
                }
            }
            catch (Exception ex) { Utils.StackTrace(ex); }
            return prefabPlayer;
        }
    }
}