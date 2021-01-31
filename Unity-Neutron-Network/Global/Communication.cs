using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Extesions;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork.Internal.Comms
{
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
                // $"The scope of the RCC({executeID}:{monoBehaviour}) is incorrect. Fix to \"void function (NeutronReader reader, bool isServer)\"
                Utils.StackTrace(ex);
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

        public static async Task<bool> ReadAsyncBytes(Stream stream, byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            try
            {
                do
                {
                    offset += bytesRead;
                    count -= bytesRead;
                }
                while (count > 0 && (bytesRead = await stream.ReadAsync(buffer, offset, count)) > 0);
                ///////////////////////////////////////////////////////////////////////////////////////
                return count == 0;
            }
            catch { return false; }
        }
    }
}