using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Server;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronRegister
    {
        public static GameObject RegisterPlayer(Neutron neutronInstance, GameObject prefabPlayer, Player mPlayer, bool isServer)
        {
            if (!isServer)
            {
                try
                {
                    if (!Utils.CheckIfLayersExists(out int client, out int server)) { Utils.LoggerError("\"ClientObject\" and \"ServerObject\" layers not exist, create it."); return null; }
                    else prefabPlayer.layer = client;
                    //------------------------------------------------------------------------------------------
                    prefabPlayer.AddComponent<ClientView>();
                    //------------------------------------------------------------------------------------------
                    prefabPlayer.name = (!mPlayer.isBot) ? mPlayer.Nickname + " -> [CLIENT OBJECT]" : mPlayer.Nickname + " -> [BOT]";
                    //------------------------------------------------------------------------------------------
                    ClientView neutronObject = prefabPlayer.GetComponent<ClientView>();
                    neutronObject.neutronProperty.ownerID = mPlayer.ID;
                    if (neutronInstance.isLocalPlayer(mPlayer))
                    {
                        neutronInstance.ClientView = neutronObject;
                        neutronObject.isMine = true;
                    }
                    //------------------------------------------------------------------------------------------
                    neutronObject._ = neutronInstance;
                    //------------------------------------------------------------------------------------------
                    neutronObject.transform.SetParent(neutronInstance.Container.transform);
                    //------------------------------------------------------------------------------------------
                    neutronInstance.neutronObjects.TryAdd(neutronObject.neutronProperty.ownerID, neutronObject);
                    //------------------------------------------------------------------------------------------
                    var nos = neutronObject.GetComponentsInChildren<NeutronBehaviour>();
                    //------------------------------------------------------------------------------------------
                    foreach (var no in nos)
                    {
                        no.IsMine = neutronObject.isMine;
                        no.ClientView = neutronObject;
                        no.IsBot = mPlayer.isBot;
                    }

                    if (neutronInstance != null)
                    {
                        if (neutronInstance.onPlayerInstantiated != null) neutronInstance.onPlayerInstantiated(mPlayer, prefabPlayer, neutronInstance);
                    }
                }
                catch (Exception ex) { Utils.LoggerError($"Register method | isServer: [{isServer}] | thrown -> {ex.Message}"); }
            }
            else if (isServer)
            {
                try
                {
                    //------------------------------------------------------------------------------------------
                    prefabPlayer.GetComponentInChildren<Renderer>().material.color = Color.black;
                    //------------------------------------------------------------------------------------------
                    if (!Utils.CheckIfLayersExists(out int client, out int server)) { Utils.LoggerError("\"ClientObject\" and \"ServerObject\" layers not exist, create it."); return null; }
                    else prefabPlayer.layer = server;
                    //------------------------------------------------------------------------------------------
                    prefabPlayer.name = (!mPlayer.isBot) ? mPlayer.Nickname + " -> [SERVER OBJECT]" : mPlayer.Nickname + " -> [BOT]";
                    //------------------------------------------------------------------------------------------
                    prefabPlayer.AddComponent<ServerView>();
                    //------------------------------------------------------------------------------------------
                    ServerView _nPState = prefabPlayer.GetComponent<ServerView>();
                    _nPState.player = mPlayer;
                    _nPState.lastPosition = prefabPlayer.transform.position;
                    //------------------------------------------------------------------------------------------
                    _nPState.transform.SetParent(Neutron.Server.Container.transform); // Container
                                                                                      //------------------------------------------------------------------------------------------
                    Neutron.Server.Players[mPlayer.tcpClient].serverView = _nPState;
                    //------------------------------------------------------------------------------------------
                    var pss = _nPState.GetComponentsInChildren<NeutronBehaviour>();
                    //------------------------------------------------------------------------------------------
                    foreach (var ps in pss)
                    {
                        ps.ServerView = _nPState;
                        ps.IsBot = mPlayer.isBot;
                    }

                    if (NeutronSFunc.onPlayerInstantiated != null) NeutronSFunc.onPlayerInstantiated(mPlayer);
                }
                catch (Exception ex) { Utils.LoggerError($"Register method | isServer: [{isServer}] | thrown -> {ex.Message}"); }
            }
            return prefabPlayer;
        }

        public static GameObject RegisterIdentity(GameObject prefabObject, int uniqueID, bool isServer)
        {
            NeutronIdentity SetID()
            {
                NeutronIdentity neutronIdentity = null;
                if (prefabObject.TryGetComponent<NeutronIdentity>(out NeutronIdentity identity))
                {
                    neutronIdentity = identity;
                    neutronIdentity.Identity.objectID = uniqueID;
                }
                else Utils.LoggerError("Unable to find Neutron Identity, this object will destroyed");

                return neutronIdentity;
            }

            if (!isServer)
            {
                NeutronIdentity identity = SetID();
                if (!identity.ServerOnly)
                {
                    int layerMask = LayerMask.NameToLayer("ClientObject");
                    if (layerMask > -1) prefabObject.layer = layerMask;
                    else
                    {
                        Utils.LoggerError("\"ClientObject\" layer not exist, create it");
                        return null;
                    }
                }
                else MonoBehaviour.Destroy(prefabObject);
            }
            else if (isServer)
            {
                SetID();
                //------------------------------------------------------------------------------------------
                prefabObject.GetComponentInChildren<Renderer>().material.color = Color.black;
                //------------------------------------------------------------------------------------------
                int layerMask = LayerMask.NameToLayer("ServerObject");
                if (layerMask > -1) prefabObject.layer = layerMask;
                else
                {
                    Utils.LoggerError("\"ServerObject\" layer not exist, create it");
                    return null;
                }
                prefabObject.name = prefabObject.name + " -> [SERVER OBJECT]";
            }
            return prefabObject;
        }
    }
}