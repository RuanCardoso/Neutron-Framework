using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Extesions;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork.Internal.Comms
{
    public class Communication
    {
        public const int BUFFER_SIZE = 1024;
        public const string PATH_SETTINGS = "\\Unity-Neutron-Network\\Resources\\neutronsettings.txt";
        public static bool InitRPC(int rpcID, byte[] parameters, Player sender, NeutronMessageInfo infor, NeutronView neutronView)
        {
            NeutronBehaviour[] neutronBehaviours = neutronView.neutronBehaviours;
            if (neutronBehaviours != null)
            {
                for (int i = 0; i < neutronBehaviours.Length; i++)
                {
                    NeutronBehaviour mInstance = neutronBehaviours[i];
                    if (mInstance != null)
                    {
                        MethodInfo Invoker = mInstance.HasRPC(rpcID, out string message);
                        if (Invoker != null)
                        {
                            object obj = Invoker.Invoke(mInstance, new object[] { new NeutronReader(parameters), sender, infor });
                            if (obj != null)
                            {
                                Type objType = obj.GetType();
                                if (objType == typeof(bool))
                                    return (bool)obj;
                            }
                            return true;
                        }
                        else { if (message != string.Empty) NeutronUtils.LoggerError(message); continue; }
                    }
                    else neutronView.ResetBehaviours();
                }
            }
            else NeutronUtils.LoggerError("Could not find any implementation of \"NeutronBehaviour\"");
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
                else { if (message != string.Empty) NeutronUtils.LoggerError(message); continue; }
            }
        }

        public static bool InitRCC(int executeID, Player sender, byte[] parameters, bool isServer, Neutron localInstance)
        {
            try
            {
                NeutronStaticBehaviour[] neutronStatics = NeutronStaticBehaviour.neutronStatics;
                for (int z = 0; z < neutronStatics.Length; z++)
                {
                    MethodInfo[] methods = neutronStatics[z].methods;
                    for (int i = 0; i < methods.Length; i++)
                    {
                        Static _static = methods[i].GetCustomAttribute<Static>();
                        if (_static != null)
                        {
                            if (_static.ID == executeID)
                            {
                                object obj = methods[i].Invoke(neutronStatics[z], new object[] { new NeutronReader(parameters), isServer, sender, localInstance });
                                if (obj != null)
                                {
                                    Type objType = obj.GetType();
                                    if (objType == typeof(GameObject))
                                    {
                                        GameObject objectToInst = (GameObject)obj;
                                        NeutronRegister.RegisterPlayer(sender, objectToInst, isServer, localInstance);
                                        if (!isServer)
                                        {
                                            Utils.MoveToContainer(objectToInst, "[Container] -> Player[Main]");
                                        }
                                        else
                                        {
                                            if (!sender.IsInRoom())
                                                Utils.MoveToContainer(objectToInst, $"[Container] -> Channel[{sender.CurrentChannel}]");
                                            else Utils.MoveToContainer(objectToInst, $"[Container] -> Room[{sender.CurrentRoom}]");
                                        }
                                    }
                                    else if (objType == typeof(bool))
                                        return (bool)obj;
                                }
                                return true;
                            }
                            else continue;
                        }
                        else continue;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                // $"The scope of the Static({executeID}:{monoBehaviour}) is incorrect. Fix to \"void function (NeutronReader reader, bool isServer)\"
                NeutronUtils.StackTrace(ex);
                return false;
            }
        }

        public static void InitResponse(int executeID, byte[] parameters)
        {
            try
            {
                NeutronStaticBehaviour[] activator = NeutronStaticBehaviour.neutronStatics;
                for (int z = 0; z < activator.Length; z++)
                {
                    MethodInfo[] methods = activator[z].methods;
                    for (int i = 0; i < methods.Length; i++)
                    {
                        Response _Response = methods[i].GetCustomAttribute<Response>();
                        if (_Response != null)
                        {
                            if (_Response.ID == executeID)
                            {
                                methods[i].Invoke(activator[z], new object[] { new NeutronReader(parameters) });
                                break;
                            }
                            else continue;
                        }
                        else continue;
                    }
                }
            }
            catch (Exception ex)
            {
                NeutronUtils.LoggerError(ex.Message);
            }
        }

        public static async Task<bool> ReadAsyncBytes(Stream stream, byte[] buffer, int offset, int count, CancellationToken token)
        {
            return await Task.Run(async () =>
            {
                int bytesRead = 0;
                try
                {
                    while (count > 0)
                    {
                        if ((bytesRead = await stream.ReadAsync(buffer, offset, count, token)) > 0)
                        {
                            offset += bytesRead;
                            count -= bytesRead;
                        }
                        else return false;
                    }
                    return count <= 0;
                }
                catch { return false; }
            });
        }
    }
}