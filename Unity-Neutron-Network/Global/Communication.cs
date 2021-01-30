using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public class Communication
{
    public const int BYTES_READ = 4096;
    public const string PATH_SETTINGS = "\\Unity-Neutron-Network\\Resources\\neutronsettings.txt";
    public static bool InitRPC(int executeID, object[] parameters, MonoBehaviour behaviour)
    {
        bool isServer = Utils.IsServer(behaviour.gameObject);
        //-----------------------------------------------------------------------------------------------------------//
        NeutronBehaviour[] scriptComponents = behaviour.GetComponentsInChildren<NeutronBehaviour>();
        //-----------------------------------------------------------------------------------------------------------//
        for (int i = 0; i < scriptComponents.Length; i++)
        {
            NeutronBehaviour mInstance = scriptComponents[i];
            MethodInfo Invoker = mInstance.HasRPC(executeID, out string message);
            if (Invoker != null)
            {
                Invoker.Invoke(mInstance, new object[] { new NeutronReader((byte[])parameters[0]), isServer });
                return true;
            }
            else { if (message != string.Empty) Utils.LoggerError(message); continue; }
        }
        return false;
    }

    public static void InitAPC(int executeID, byte[] parameters, MonoBehaviour behaviour)
    {
        NeutronBehaviour[] scriptComponents = behaviour.GetComponentsInChildren<NeutronBehaviour>();
        //-----------------------------------------------------------------------------------------------------------//
        for (int i = 0; i < scriptComponents.Length; i++)
        {
            NeutronBehaviour mInstance = scriptComponents[i];
            MethodInfo Invoker = mInstance.HasAPC(executeID, out string message);
            if (Invoker != null)
            {
                Invoker.Invoke(mInstance, new object[] { new NeutronReader(parameters) });
                break;
            }
            else { if (message != string.Empty) Utils.LoggerError(message); continue; }
        }
    }

    public static bool InitRCC(string monoBehaviour, int executeID, Player sender, byte[] parameters, bool isServer, Neutron localInstance)
    {
        try
        {
            var activator = GameObject.FindObjectOfType<NeutronStatic>();
            //--------------------------------------------------------------------------------------------------------------
            MethodInfo[] methods = activator.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //--------------------------------------------------------------------------------------------------------------
            if (activator == null)
            {
                Utils.LoggerError($"Unable to find {activator.name} object, use DontDestroyOnLoad");
                return false;
            }
            //--------------------------------------------------------------------------------------------------------------
            for (int i = 0; i < methods.Length; i++)
            {
                RCC rcc = methods[i].GetCustomAttribute<RCC>();
                if (rcc != null)
                {
                    if (rcc.ID == executeID)
                    {
                        methods[i].Invoke(activator, new object[] { new NeutronReader(parameters), isServer, sender, localInstance });
                        return true;
                    }
                    else continue;
                }
                else continue;
            }
            return false;
        }
        catch (Exception ex)
        {
            Utils.LoggerError($"The scope of the RCC({executeID}:{monoBehaviour}) is incorrect. Fix to \"void function (NeutronReader reader, bool isServer)\"");
            return false;
        }
    }

    public static void InitACC(string monoBehaviour, int executeID, byte[] parameters)
    {
        try
        {
            var activator = GameObject.FindObjectOfType<NeutronStatic>();
            //--------------------------------------------------------------------------------------------------------------
            MethodInfo[] methods = activator.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            //--------------------------------------------------------------------------------------------------------------
            if (activator == null)
            {
                Utils.LoggerError($"Unable to find {activator.name} object, use DontDestroyOnLoad");
                return;
            }
            //--------------------------------------------------------------------------------------------------------------
            for (int i = 0; i < methods.Length; i++)
            {
                ACC acc = methods[i].GetCustomAttribute<ACC>();
                if (acc != null)
                {
                    if (acc.ID == executeID)
                    {
                        methods[i].Invoke(activator, new object[] { new NeutronReader(parameters) });
                        break;
                    }
                    else continue;
                }
                else continue;
            }
        }
        catch (Exception ex)
        {
            Utils.LoggerError(ex.Message);
        }
    }

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

    public static async Task<bool> ReadAsyncBytes(Stream stream, byte[] buffer, int offset, int count)
    {
        int bytesRead;
        try
        {
            while (count > 0 && (bytesRead = await stream.ReadAsync(buffer, offset, count)) > 0)
            {
                offset += bytesRead;
                count -= bytesRead;
            }
            return count == 0;
        }
        catch { return false; }
    }
}