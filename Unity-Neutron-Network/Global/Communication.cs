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
        public static bool InitRPC(int executeID, object[] parameters, MonoBehaviour behaviour)
        {
            //-----------------------------------------------------------------------------------------------------------//
            NeutronBehaviour[] scriptComponents = behaviour.GetComponentsInChildren<NeutronBehaviour>();
            //-----------------------------------------------------------------------------------------------------------//
            for (int i = 0; i < scriptComponents.Length; i++)
            {
                NeutronBehaviour mInstance = scriptComponents[i];
                MethodInfo Invoker = mInstance.HasRPC(executeID, out string message);
                if (Invoker != null)
                {
                    object obj = Invoker.Invoke(mInstance, new object[] { new NeutronReader((byte[])parameters[0]) });
                    if (obj != null)
                    {
                        Type objType = obj.GetType();
                        if (objType == typeof(bool))
                            return (bool)obj;
                    }
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

        public static bool InitRCC(int executeID, Player sender, byte[] parameters, bool isServer, Neutron localInstance)
        {
            try
            {
                NeutronStatic[] activator = NeutronStatic.neutronStatics;
                for (int z = 0; z < activator.Length; z++)
                {
                    MethodInfo[] methods = activator[z].methodInfos;
                    for (int i = 0; i < methods.Length; i++)
                    {
                        Static _static = methods[i].GetCustomAttribute<Static>();
                        if (_static != null)
                        {
                            if (_static.ID == executeID)
                            {
                                object obj = methods[i].Invoke(activator[z], new object[] { new NeutronReader(parameters), isServer, sender, localInstance });
                                if (obj != null)
                                {
                                    Type objType = obj.GetType();
                                    if (objType == typeof(GameObject))
                                    {
                                        GameObject objectToInst = (GameObject)obj;
                                        NeutronRegister.RegisterPlayer(localInstance, objectToInst, sender, isServer);
                                        if (!isServer)
                                        {
                                            Utils.MoveToContainer(objectToInst, "[Container] -> Player[Main]");
                                        }
                                        else
                                        {
                                            if (!sender.IsInRoom())
                                                Utils.MoveToContainer(objectToInst, $"[Container] -> Channel[{sender.currentChannel}]");
                                            else Utils.MoveToContainer(objectToInst, $"[Container] -> Room[{sender.currentRoom}]");
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
                Utils.StackTrace(ex);
                return false;
            }
        }

        public static void InitResponse(int executeID, byte[] parameters)
        {
            try
            {
                NeutronStatic[] activator = NeutronStatic.neutronStatics;
                for (int z = 0; z < activator.Length; z++)
                {
                    MethodInfo[] methods = activator[z].methodInfos;
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
                Utils.LoggerError(ex.Message);
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